using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

/// <summary>Registro de preguntas, calificación opcional 👍/👎 y tope para no llenar la base.</summary>
public class NegAsistenteRegistroTests
{
    private const int Dni = 46110009;

    private sealed class OpenAiFijo(string texto) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":" + System.Text.Json.JsonSerializer.Serialize(texto) + "}}]," +
                    "\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5}}",
                    Encoding.UTF8, "application/json")
            });
    }

    private sealed class FabricaHttp(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://openai-falso/v1/") };
    }

    private static NegAsistente Crear(ApplicationDbContext db, bool conClave)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = conClave ? "true" : "false",
            ["Asistente:ApiKey"] = "sk-test",
            ["Asistente:MaxPreguntasDia"] = "0",
        }).Build();

        var insumos = new Mock<INegInsumos>();
        insumos.Setup(i => i.obtenerInsumos()).Returns(new List<INSUMO>
        {
            new() { idInsumo = 1, nombre = "Chupetes", stockActual = 79, stockMinimo = 50, stockMaximo = 200 },
        });

        return new NegAsistente(config, Mock.Of<INegUsuarios>(), Mock.Of<INegDashboard>(), Mock.Of<INegBebes>(),
            insumos.Object, Mock.Of<INegAsistencia>(), Mock.Of<INegVoluntaria>(), Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), db, new FabricaHttp(new OpenAiFijo("Hay chupetes de sobra en el depósito.")));
    }

    private static Task<AsistentePreguntaRespuesta> Preguntar(NegAsistente neg, string pregunta) =>
        neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = pregunta });

    [Fact]
    public async Task CadaPregunta_QuedaRegistrada_ConSuResultado()
    {
        var db = TestDb.Crear();
        var neg = Crear(db, conClave: true);

        var r = await Preguntar(neg, "hay chupetes?");

        Assert.NotNull(r.IdRegistro);
        var reg = db.ASISTENTEREGISTRO.Single(x => x.idAsistenteRegistro == r.IdRegistro);
        Assert.Equal(Dni, reg.dni);
        Assert.Equal("hay chupetes?", reg.pregunta);
        Assert.Equal(r.Respuesta, reg.respuesta);
        Assert.Equal("ok", reg.resultado);
        Assert.Equal(15, reg.tokens);
        Assert.Null(reg.calificacion);
    }

    [Fact]
    public async Task SinModelo_SeRegistraComoRespaldo()
    {
        var db = TestDb.Crear();
        var r = await Preguntar(Crear(db, conClave: false), "cuantos chupetes hay");

        Assert.Equal("respaldo", db.ASISTENTEREGISTRO.Single(x => x.idAsistenteRegistro == r.IdRegistro).resultado);
    }

    [Fact]
    public async Task Calificar_EsOpcional_YSePuedeCambiarOQuitar()
    {
        var db = TestDb.Crear();
        var neg = Crear(db, conClave: true);
        var id = (await Preguntar(neg, "hay chupetes?")).IdRegistro!.Value;

        neg.Calificar(Dni, new AsistenteCalificarRequest { IdRegistro = id, Calificacion = -1, Comentario = "  no dijo cuántos  " });
        var reg = db.ASISTENTEREGISTRO.Single(x => x.idAsistenteRegistro == id);
        Assert.Equal((short)-1, reg.calificacion);
        Assert.Equal("no dijo cuántos", reg.comentario);

        neg.Calificar(Dni, new AsistenteCalificarRequest { IdRegistro = id, Calificacion = 1 });
        Assert.Equal((short)1, reg.calificacion);
        Assert.Null(reg.comentario);

        neg.Calificar(Dni, new AsistenteCalificarRequest { IdRegistro = id, Calificacion = 0 });
        Assert.Null(reg.calificacion);
    }

    [Fact]
    public async Task Calificar_NoDejaVotarRespuestasAjenasNiValoresRaros()
    {
        var db = TestDb.Crear();
        var neg = Crear(db, conClave: true);
        var id = (await Preguntar(neg, "hay chupetes?")).IdRegistro!.Value;

        Assert.Throws<NotFoundException>(() =>
            neg.Calificar(12345678, new AsistenteCalificarRequest { IdRegistro = id, Calificacion = 1 }));
        Assert.Throws<ApplicationException>(() =>
            neg.Calificar(Dni, new AsistenteCalificarRequest { IdRegistro = id, Calificacion = 5 }));
    }

    [Fact]
    public async Task Tope_BorraLasMasViejas_PeroConservaLasMarcadasMal()
    {
        var db = TestDb.Crear();
        for (var i = 0; i < 5; i++)
            db.ASISTENTEREGISTRO.Add(new ASISTENTEREGISTRO { dni = Dni, pregunta = $"mala {i}", calificacion = -1, fecha = DateTime.UtcNow });
        for (var i = 0; i < NegAsistente.MaxRegistros + 3; i++)
            db.ASISTENTEREGISTRO.Add(new ASISTENTEREGISTRO { dni = Dni, pregunta = $"vieja {i}", fecha = DateTime.UtcNow });
        db.SaveChanges();

        var r = await Preguntar(Crear(db, conClave: true), "hay chupetes?");

        Assert.Equal(NegAsistente.MaxRegistros, db.ASISTENTEREGISTRO.Count(x => x.calificacion != -1));
        Assert.Equal(5, db.ASISTENTEREGISTRO.Count(x => x.calificacion == -1));
        Assert.Contains(db.ASISTENTEREGISTRO, x => x.idAsistenteRegistro == r.IdRegistro);
        Assert.DoesNotContain(db.ASISTENTEREGISTRO, x => x.pregunta == "vieja 0");
    }

    [Fact]
    public async Task Registros_SoloMalas_DevuelveVotosNegativosYRespaldos()
    {
        var db = TestDb.Crear();
        var conModelo = Crear(db, conClave: true);
        var bien = (await Preguntar(conModelo, "hay chupetes?")).IdRegistro!.Value;
        var mal = (await Preguntar(conModelo, "y pañales?")).IdRegistro!.Value;
        conModelo.Calificar(Dni, new AsistenteCalificarRequest { IdRegistro = mal, Calificacion = -1 });
        var respaldo = (await Preguntar(Crear(db, conClave: false), "cuantos chupetes hay")).IdRegistro!.Value;

        var malas = conModelo.ObtenerRegistros(Dni, soloMalas: true, limite: 50).Select(x => x.IdRegistro).ToList();

        Assert.Equal(new[] { respaldo, mal }, malas);
        Assert.DoesNotContain(bien, malas);
        Assert.Equal(3, conModelo.ObtenerRegistros(Dni, soloMalas: false, limite: 50).Count);
    }
}
