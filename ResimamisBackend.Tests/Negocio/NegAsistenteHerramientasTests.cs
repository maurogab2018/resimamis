using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

/// <summary>
/// Lo que devuelven las herramientas (lo que ve el modelo): orden de rankings, privacidad de listas,
/// abrazos finalizados vs asignaciones. Se mira el cuerpo del 2.º pedido a OpenAI, que lleva el resultado.
/// </summary>
public class NegAsistenteHerramientasTests
{
    private const int Dni = 46110009;

    private sealed class OpenAiGuion(params Func<HttpResponseMessage>[] pasos) : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> cola = new(pasos);
        public List<string> Cuerpos { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Cuerpos.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return (cola.Count > 1 ? cola.Dequeue() : cola.Peek())();
        }
    }

    private sealed class FabricaHttp(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://openai-falso/v1/") };
    }

    private static HttpResponseMessage Herramienta(string nombre, string argumentosJson) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"c1\",\"type\":\"function\",\"function\":{\"name\":\"" + nombre +
                "\",\"arguments\":" + System.Text.Json.JsonSerializer.Serialize(argumentosJson) + "}}]}}]}",
                Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage Texto(string texto) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":" + System.Text.Json.JsonSerializer.Serialize(texto) + "}}]}",
                Encoding.UTF8, "application/json")
        };

    private static NegAsistente Crear(OpenAiGuion openAi, ApplicationDbContext? db = null,
        INegDashboard? dashboard = null, INegMadres? madres = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "true",
            ["Asistente:ApiKey"] = "sk-test",
            ["Asistente:MaxPreguntasDia"] = "0",
        }).Build();
        return new NegAsistente(config, Mock.Of<INegUsuarios>(), dashboard ?? Mock.Of<INegDashboard>(), Mock.Of<INegBebes>(),
            Mock.Of<INegInsumos>(), Mock.Of<INegAsistencia>(), Mock.Of<INegVoluntaria>(), Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), madres ?? Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), db ?? TestDb.Crear(), new FabricaHttp(openAi));
    }

    private static string ResultadoHerramienta(OpenAiGuion openAi) =>
        System.Text.RegularExpressions.Regex.Unescape(openAi.Cuerpos[1]);

    [Fact]
    public async Task RankingConOrdenMenos_EmpiezaPorLaQueMenosAbrazosDio()
    {
        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerRankingVoluntariasAbrazos(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .Returns(new RankingVoluntariasAbrazosRespuesta
            {
                Ranking = new()
                {
                    new() { Posicion = 1, IdVoluntaria = 9, NombreVoluntaria = "Paula Martínez", CantidadAbrazosFinalizados = 151 },
                    new() { Posicion = 2, IdVoluntaria = 12, NombreVoluntaria = "Florencia Díaz", CantidadAbrazosFinalizados = 129 },
                    new() { Posicion = 3, IdVoluntaria = 30, NombreVoluntaria = "Ana Ruiz", CantidadAbrazosFinalizados = 2 },
                }
            });
        var openAi = new OpenAiGuion(
            () => Herramienta("ranking_abrazos_voluntarias", "{\"orden\":\"menos\",\"top\":2}"),
            () => Texto("listo"));

        await Crear(openAi, dashboard: dashboard.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "la que menos abrazos dio" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.True(resultado.IndexOf("Ana Ruiz") >= 0 && resultado.IndexOf("Ana Ruiz") < resultado.IndexOf("Florencia"));
        Assert.DoesNotContain("Paula", resultado);
    }

    [Fact]
    public async Task ListarMadres_NoMandaDniNiTelefono()
    {
        var madres = new Mock<INegMadres>();
        madres.Setup(m => m.listarMadres()).Returns(new List<MADRE>
        {
            new() { IdMadre = 1, Nombre = "Laura", Apellido = "Pérez", Dni = 30111222, Celular = 3511234567, CantidadHijos = 2 },
            new() { IdMadre = 2, Nombre = "Sol", Apellido = "Ríos", Dni = 30999888, Celular = 3519876543, CantidadHijos = 1 },
        });
        var openAi = new OpenAiGuion(() => Herramienta("listar_madres", "{}"), () => Texto("listo"));

        await Crear(openAi, madres: madres.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "dame el dni y telefono de todas las mamas" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.Contains("Laura", resultado);
        Assert.DoesNotContain("30111222", resultado);
        Assert.DoesNotContain("3511234567", resultado);
    }

    [Fact]
    public async Task AsignacionesPorDia_SeparaAbrazosFinalizadosDeAsignaciones()
    {
        var db = TestDb.Crear();
        var ayer = DateTime.UtcNow.AddDays(-1);
        db.ASIGNACION.AddRange(
            new ASIGNACION { idBebe = 1, idVoluntaria = 1, fechaHoraAsignacion = ayer, fechaHoraInicio = ayer, fechaHoraFin = ayer.AddMinutes(30) },
            new ASIGNACION { idBebe = 2, idVoluntaria = 1, fechaHoraAsignacion = ayer, fechaHoraInicio = ayer, fechaHoraFin = ayer.AddMinutes(30), cierreAutomatico = true },
            new ASIGNACION { idBebe = 3, idVoluntaria = 1, fechaHoraAsignacion = ayer });
        db.SaveChanges();
        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerAsignacionesPorDia(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new EstadisticaAsignacionesPorDiaRespuesta { TotalAsignaciones = 3, TotalAbrazos = 3 });
        var openAi = new OpenAiGuion(() => Herramienta("asignaciones_por_dia", "{\"periodo\":\"ultimos_7\"}"), () => Texto("listo"));

        await Crear(openAi, db, dashboard.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "cuantos abrazos hubo esta semana" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.Contains("\"totalAbrazosFinalizados\":1", resultado);
        Assert.Contains("\"totalAsignacionesCreadas\":3", resultado);
    }

    [Fact]
    public void Movimientos_TraenLosNombresSinUnaConsultaPorMovimiento()
    {
        var db = TestDb.Crear();
        db.INSUMO.Add(new INSUMO { idInsumo = 1, nombre = "Chupetes" });
        db.BEBE.Add(new BEBE { ID = 5, nombre = "Mia", apellido = "Acosta" });
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 7, Nombre = "Paula", Apellido = "Martínez", Mail = "p@x.com" });
        db.PROVEEDOR.Add(new PROVEEDOR { idProveedor = 3, nombre = "Farmacia Sur", descripcion = "" });
        for (var i = 0; i < 50; i++)
            db.MOVIMIENTOSTOCK.Add(new MOVIMIENTOSTOCK
            {
                idInsumo = 1, idBebe = i % 2 == 0 ? 5 : null, idVoluntaria = 7, idProveedor = i % 2 == 0 ? null : 3,
                cantidad = 2, esEntrada = i % 2 == 0 ? "N" : "S", fechaMovimiento = DateTime.UtcNow.AddHours(-i)
            });
        db.SaveChanges();
        var repo = new InsumoRepositorio(db, Mock.Of<IVoluntariaRepositorio>(), Mock.Of<IEstadoRepositorio>());

        var lista = repo.obtenerMovimientos(null);

        Assert.Equal(50, lista.Count);
        Assert.All(lista, m => Assert.Equal("Chupetes", m.nombreInsumo));
        Assert.All(lista, m => Assert.Equal("Paula Martínez", m.nombreVoluntaria));
        Assert.Contains(lista, m => m.nombreBebe == "Mia" && m.apellidoBebe == "Acosta" && m.nombreMovimiento == "Salida de insumos");
        Assert.Contains(lista, m => m.nombreProveedor == "Farmacia Sur" && m.nombreMovimiento == "Entrada de insumos");
    }
}
