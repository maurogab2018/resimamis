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

/// <summary>
/// El asistente nunca puede dejar a la coordinadora con un error: sin clave, con OpenAI caído,
/// con 429/500 o con respuesta vacía tiene que contestar con datos reales de Resimamis.
/// </summary>
public class NegAsistenteRobustezTests
{
    private const int Dni = 46110009;

    private sealed class OpenAiFalso : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> respuestas;
        public int Llamadas { get; private set; }
        public List<string> Cuerpos { get; } = new();

        public OpenAiFalso(params Func<HttpResponseMessage>[] respuestas) =>
            this.respuestas = new Queue<Func<HttpResponseMessage>>(respuestas);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Llamadas++;
            Cuerpos.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return Siguiente();
        }

        private HttpResponseMessage Siguiente()
        {
            var r = respuestas.Count > 1 ? respuestas.Dequeue() : respuestas.Peek();
            return r();
        }
    }

    private sealed class FabricaHttp(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://openai-falso/v1/") };
    }

    private static HttpResponseMessage Error(HttpStatusCode code) =>
        new(code) { Content = new StringContent("{\"error\":{\"message\":\"falla simulada\"}}", Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Texto(string texto) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":" + System.Text.Json.JsonSerializer.Serialize(texto) + "}}]," +
                "\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5}}",
                Encoding.UTF8, "application/json")
        };

    private static NegAsistente Crear(HttpMessageHandler handler, bool conClave = true)
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
            new() { idInsumo = 2, nombre = "Pañales", stockActual = 210, stockMinimo = 50, stockMaximo = 500 },
            new() { idInsumo = 3, nombre = "Toallitas húmedas", stockActual = 24, stockMinimo = 30, stockMaximo = 300 },
            new() { idInsumo = 4, nombre = "Leche fórmula", descripcion = "Lata 400g", stockActual = 44, stockMinimo = 40, stockMaximo = 100 },
        });
        insumos.Setup(i => i.obtenerInsumosBajoStockMinimo()).Returns(new List<InsumoBajoStockMinimo>
        {
            new() { nombre = "Toallitas húmedas", stockActual = 24, stockMinimo = 30 }
        });

        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerCoordinacionHoy()).Returns(new DashboardCoordinacionHoyRespuesta
        {
            Fecha = new DateOnly(2026, 11, 21),
            BebesActivos = 24,
            AbrazosHoy = new AbrazosHoyResumen { Finalizados = 3 },
            VisitasHoy = 2
        });

        var bebes = new Mock<INegBebes>();
        bebes.Setup(b => b.listarBebes()).Returns(new List<BEBE>
        {
            new() { ID = 1, nombre = "Mia", apellido = "Acosta" },
            new() { ID = 2, nombre = "Luca", apellido = "Pérez" },
        });

        return new NegAsistente(
            config,
            Mock.Of<INegUsuarios>(),
            dashboard.Object,
            bebes.Object,
            insumos.Object,
            Mock.Of<INegAsistencia>(),
            Mock.Of<INegVoluntaria>(),
            Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(),
            Mock.Of<INegMadres>(),
            Mock.Of<INegSalas>(),
            Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(),
            Mock.Of<INegGenericos>(),
            Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(),
            TestDb.Crear(),
            new FabricaHttp(handler));
    }

    private static Task<AsistentePreguntaRespuesta> Preguntar(NegAsistente neg, string pregunta) =>
        neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = pregunta });

    [Fact]
    public async Task SinClave_IgualRespondeElStockDeChupetes()
    {
        var openAi = new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError));
        var r = await Preguntar(Crear(openAi, conClave: false), "cuantos chupetes hay");

        Assert.Contains("79", r.Respuesta);
        Assert.Contains("Chupetes", r.Respuesta);
        Assert.Equal(0, openAi.Llamadas);
    }

    [Theory]
    [InlineData("chupte", "Chupetes")]
    [InlineData("cuántos chupete hay?", "Chupetes")]
    [InlineData("hay chupetines", "Chupetes")]
    [InlineData("xupete", "Chupetes")]
    [InlineData("chpte", "Chupetes")]
    [InlineData("CHUPETEZ", "Chupetes")]
    [InlineData("cuantos chupetes ay", "Chupetes")]
    [InlineData("pañal", "Pañales")]
    [InlineData("panales", "Pañales")]
    [InlineData("panyales", "Pañales")]
    [InlineData("toayitas", "Toallitas")]
    [InlineData("toallas humedas", "Toallitas")]
    [InlineData("tohallitas", "Toallitas")]
    [InlineData("lehe", "Leche")]
    [InlineData("cuanta formula queda", "Leche")]
    public async Task ErroresDeTipeo_EncuentranElInsumo(string pregunta, string esperado)
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError)), conClave: false), pregunta);
        Assert.Contains(esperado, r.Respuesta);
    }

    [Theory]
    [InlineData("gasas")]
    [InlineData("cuantas jeringas hay")]
    public async Task InsumoQueNoExiste_IgualRespondeConElStock(string pregunta)
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError)), conClave: false), pregunta);
        Assert.False(string.IsNullOrWhiteSpace(r.Respuesta));
        Assert.Contains("Resimamis", r.Respuesta);
    }

    [Fact]
    public async Task OpenAiCaido_ReintentaYDespuesRespondeConDatos()
    {
        var openAi = new OpenAiFalso(() => Error(HttpStatusCode.ServiceUnavailable));
        var r = await Preguntar(Crear(openAi), "cuantos pañales quedan");

        Assert.Contains("210", r.Respuesta);
        Assert.Equal(3, openAi.Llamadas);
        Assert.Contains("respaldo_datos_sistema", r.HerramientasUsadas);
    }

    [Fact]
    public async Task PrimerLlamadoFalla_ElReintentoUsaLaRespuestaDelModelo()
    {
        var openAi = new OpenAiFalso(
            () => Error(HttpStatusCode.TooManyRequests),
            () => Texto("Hay **79 chupetes** en el depósito."));
        var r = await Preguntar(Crear(openAi), "cuantos chupetes hay");

        Assert.Equal("Hay **79 chupetes** en el depósito.", r.Respuesta);
        Assert.Equal(2, openAi.Llamadas);
    }

    [Fact]
    public async Task ClaveInvalida_NoReintentaYRespondeConDatos()
    {
        var openAi = new OpenAiFalso(() => Error(HttpStatusCode.Unauthorized));
        var r = await Preguntar(Crear(openAi), "que insumos faltan");

        Assert.Equal(1, openAi.Llamadas);
        Assert.Contains("Toallitas", r.Respuesta);
    }

    [Fact]
    public async Task ModeloDevuelveVacio_RespondeConDatos()
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Texto(""))), "cuantos bebes hay");
        Assert.Contains("2 bebés activos", r.Respuesta);
    }

    [Fact]
    public async Task RespuestaIlegible_RespondeConDatos()
    {
        var openAi = new OpenAiFalso(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>502</html>") });
        var r = await Preguntar(Crear(openAi), "como esta el dia de hoy");
        Assert.Contains("24", r.Respuesta);
        Assert.False(string.IsNullOrWhiteSpace(r.Respuesta));
    }

    private static HttpResponseMessage LlamarHerramienta(string nombre, string argumentosJson) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"c1\",\"type\":\"function\",\"function\":{\"name\":\"" + nombre +
                "\",\"arguments\":" + System.Text.Json.JsonSerializer.Serialize(argumentosJson) + "}}]}}]}",
                Encoding.UTF8, "application/json")
        };

    [Theory]
    [InlineData("paula martinez")]
    [InlineData("PAULA MARTINES")]
    [InlineData("martínez paula")]
    public async Task BuscarVoluntaria_SinTildesNiOrden_LaEncuentra(string texto)
    {
        var voluntarias = new Mock<INegVoluntaria>();
        voluntarias.Setup(v => v.listarVoluntarias()).Returns(new List<VOLUNTARIA>
        {
            new() { IdVoluntaria = 5, Nombre = "Paula", Apellido = "Martínez" },
            new() { IdVoluntaria = 6, Nombre = "Florencia", Apellido = "Díaz" },
        });
        var openAi = new OpenAiFalso(
            () => LlamarHerramienta("buscar_voluntarias", "{\"texto\":\"" + texto + "\"}"),
            () => Texto("listo"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "true",
            ["Asistente:ApiKey"] = "sk-test",
        }).Build();
        var neg = new NegAsistente(config, Mock.Of<INegUsuarios>(), Mock.Of<INegDashboard>(), Mock.Of<INegBebes>(),
            Mock.Of<INegInsumos>(), Mock.Of<INegAsistencia>(), voluntarias.Object, Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), TestDb.Crear(), new FabricaHttp(openAi));

        await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "quien es " + texto });

        // Lo que la herramienta le devolvió al modelo va en el segundo pedido.
        var segundo = openAi.Cuerpos[1];
        Assert.Contains("\\u0022idVoluntaria\\u0022:5", segundo);
        Assert.DoesNotContain("\\u0022idVoluntaria\\u0022:6", segundo);
    }

    [Fact]
    public async Task Abreviado_VoluntLibsHoy_ListaLasLibres()
    {
        var voluntarias = new Mock<INegVoluntaria>();
        voluntarias.Setup(v => v.listarVoluntariasLibres1()).Returns(new List<VOLUNTARIA>
        {
            new() { IdVoluntaria = 5, Nombre = "Paula", Apellido = "Martínez" }
        });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "false",
        }).Build();
        var neg = new NegAsistente(config, Mock.Of<INegUsuarios>(), Mock.Of<INegDashboard>(), Mock.Of<INegBebes>(),
            Mock.Of<INegInsumos>(), Mock.Of<INegAsistencia>(), voluntarias.Object, Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), TestDb.Crear(), new FabricaHttp(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError))));

        var r = await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "volunt libs hoy" });
        Assert.Contains("Paula Martínez", r.Respuesta);
        Assert.Contains("libres", r.Respuesta);
    }

    [Theory]
    [InlineData("cual es la capital de francia")]
    [InlineData("asdfghjkl")]
    [InlineData("hola")]
    public async Task FueraDeTema_SinModelo_IgualHablaDeResimamis(string pregunta)
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError)), conClave: false), pregunta);
        Assert.Contains("Resimamis", r.Respuesta);
    }

    [Fact]
    public async Task TodoFalla_IgualDevuelveUnMensajeAmable()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "false",
        }).Build();
        var roto = new Mock<INegDashboard>();
        roto.Setup(d => d.ObtenerCoordinacionHoy()).Throws(new Exception("base caída"));
        var neg = new NegAsistente(config, Mock.Of<INegUsuarios>(), roto.Object, Mock.Of<INegBebes>(),
            Mock.Of<INegInsumos>(), Mock.Of<INegAsistencia>(), Mock.Of<INegVoluntaria>(), Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), TestDb.Crear(), new FabricaHttp(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError))));

        var r = await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "como viene el dia" });
        Assert.Contains("Resimamis", r.Respuesta);
    }
}
