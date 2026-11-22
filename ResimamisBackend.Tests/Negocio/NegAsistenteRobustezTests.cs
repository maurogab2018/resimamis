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
            () => Texto("Hay chupetes de sobra en el depósito."));
        var r = await Preguntar(Crear(openAi), "cuantos chupetes hay");

        Assert.Equal("Hay chupetes de sobra en el depósito.", r.Respuesta);
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

    private static (NegAsistente neg, OpenAiFalso openAi, Mock<INegBebes> bebes) CrearConBebes(params Func<HttpResponseMessage>[] respuestas)
    {
        var db = TestDb.Crear();
        db.BEBE.AddRange(
            new BEBE { ID = 16, nombre = "Mia", apellido = "Acosta" },
            new BEBE { ID = 40, nombre = "Lucas", apellido = "González", FechaSalida = new DateTime(2026, 10, 1) });
        db.SaveChanges();
        var bebes = new Mock<INegBebes>();
        bebes.Setup(b => b.consultarBebe(16)).Returns(new BEBE
        {
            ID = 16, nombre = "Mia", apellido = "Acosta", PesoNacimiento = 1320,
            Madre = new MADRE { IdMadre = 16, Nombre = "Florencia", Apellido = "Acosta" }
        });
        bebes.Setup(b => b.consultarBebe(40)).Returns(new BEBE { ID = 40, nombre = "Lucas", apellido = "González", PesoNacimiento = 1850 });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "true",
            ["Asistente:ApiKey"] = "sk-test",
        }).Build();
        var openAi = new OpenAiFalso(respuestas);
        var neg = new NegAsistente(config, Mock.Of<INegUsuarios>(), Mock.Of<INegDashboard>(), bebes.Object,
            Mock.Of<INegInsumos>(), Mock.Of<INegAsistencia>(), Mock.Of<INegVoluntaria>(), Mock.Of<INegAsignacion>(),
            Mock.Of<INegVisitas>(), Mock.Of<INegMadres>(), Mock.Of<INegSalas>(), Mock.Of<INegTareas>(),
            Mock.Of<INegProveedores>(), Mock.Of<INegGenericos>(), Mock.Of<INegRegistroActividad>(),
            Mock.Of<INegNotificaciones>(), db, new FabricaHttp(openAi));
        return (neg, openAi, bebes);
    }

    [Fact]
    public async Task IdEquivocadoConNombre_GanaElNombre_NuncaOtroBebe()
    {
        // El modelo pide la ficha con el id de Lucas pero el nombre de Mia: se tiene que consultar a Mia.
        var (neg, openAi, bebes) = CrearConBebes(
            () => LlamarHerramienta("ficha_bebe", "{\"id_bebe\":40,\"nombre_bebe\":\"mia acosta\"}"),
            () => Texto("Mia Acosta pesó 1320 g al nacer."));
        var r = await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "peso de mia acosta" });

        bebes.Verify(b => b.consultarBebe(16), Times.Once);
        bebes.Verify(b => b.consultarBebe(40), Times.Never);
        Assert.Contains("[Mia Acosta](bebe:16)", r.Respuesta);
    }

    [Fact]
    public async Task NombreQueNoExiste_DevuelveErrorYNoConsultaOtro()
    {
        var (neg, openAi, bebes) = CrearConBebes(
            () => LlamarHerramienta("ficha_bebe", "{\"id_bebe\":40,\"nombre_bebe\":\"Juana Inexistente\"}"),
            () => Texto("No encontré a Juana."));
        await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "ficha de juana inexistente" });

        bebes.Verify(b => b.consultarBebe(It.IsAny<int>()), Times.Never);
        Assert.Contains("NO muestres datos de otra persona", openAi.Cuerpos[1]);
    }

    [Fact]
    public async Task BuscarMadreConNombreDeBebe_DevuelveLaMamaDeEseBebe()
    {
        var (neg, openAi, _) = CrearConBebes(
            () => LlamarHerramienta("buscar_madres", "{\"texto\":\"mia acosta\"}"),
            () => Texto("listo"));
        await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "y la mama?" });

        Assert.Contains("Florencia", openAi.Cuerpos[1]);
        Assert.Contains("es un beb", openAi.Cuerpos[1]);
    }

    [Fact]
    public async Task RespuestaSinLink_ElServidorAgregaElLink()
    {
        var (neg, _, _) = CrearConBebes(
            () => LlamarHerramienta("ficha_bebe", "{\"id_bebe\":16}"),
            () => Texto("La bebé **Mia Acosta** pesó 1320 g."));
        var r = await neg.Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "bebe mia acosta" });

        Assert.Equal("La bebé **[Mia Acosta](bebe:16)** pesó 1320 g.", r.Respuesta);
    }

    [Fact]
    public async Task NumeroInventadoSinHerramientas_ExigeConsultarYUsaElDatoReal()
    {
        var openAi = new OpenAiFalso(
            () => Texto("El insumo bajo el mínimo es **Gasas**: 68 unidades (mínimo 100)."),
            () => LlamarHerramienta("insumos_bajo_stock", "{}"),
            () => Texto("El insumo bajo el mínimo es **Toallitas húmedas**: 24 (mínimo 30)."));
        var r = await Preguntar(Crear(openAi), "cual esta bajo el minimo?");

        Assert.Contains("Toallitas", r.Respuesta);
        Assert.Contains("\"tool_choice\":\"required\"", openAi.Cuerpos[1]);
        Assert.Contains("68", openAi.Cuerpos[1]);
    }

    [Fact]
    public async Task CuentaConDatosRespaldados_NoSeRechaza()
    {
        var openAi = new OpenAiFalso(() => Texto("Hay 24 toallitas; faltan **276** para el máximo de 300."));
        var neg = Crear(openAi);
        await neg.Preguntar(Dni, new AsistentePreguntaRequest
        {
            Pregunta = "cuanto falta para el maximo",
            Historial = new List<AsistenteMensaje>
            {
                new() { Rol = "asistente", Contenido = "Toallitas húmedas: 24 (mínimo 30, máximo 300)." },
            }
        });
        Assert.Equal(1, openAi.Llamadas);
    }

    [Fact]
    public async Task NumeroQueVieneDelHistorial_NoSeRechaza()
    {
        var openAi = new OpenAiFalso(() => Texto("Mia Acosta pesó **1320 gramos** al nacer."));
        var neg = Crear(openAi);
        var r = await neg.Preguntar(Dni, new AsistentePreguntaRequest
        {
            Pregunta = "peso",
            Historial = new List<AsistenteMensaje>
            {
                new() { Rol = "usuario", Contenido = "bebe mia acosta" },
                new() { Rol = "asistente", Contenido = "[Mia Acosta](bebe:16): peso al nacer 1320 gramos." },
            }
        });

        Assert.Equal(1, openAi.Llamadas);
        Assert.Contains("1320", r.Respuesta);
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

    [Fact]
    public async Task PremisaFalsa_NoRepiteQueLasMadresMurieron()
    {
        var openAi = new OpenAiFalso(
            () => Texto("Hay 7 madres dadas de baja y todas han fallecido."),
            () => Texto("Hay 7 madres dadas de baja. El sistema solo indica que ya no están activas."));
        var r = await Preguntar(Crear(openAi), "madres muertas hay 8");

        Assert.Equal(2, openAi.Llamadas);
        Assert.DoesNotContain("fallec", r.Respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NO registra fallecimientos", openAi.Cuerpos[1]);
    }

    [Fact]
    public async Task PremisaFalsa_SiInsiste_SeAgregaLaAclaracion()
    {
        var openAi = new OpenAiFalso(() => Texto("Las madres dadas de baja murieron."));
        var r = await Preguntar(Crear(openAi), "madres muertas");

        Assert.Equal(2, openAi.Llamadas);
        Assert.Contains("no registra fallecimientos", r.Respuesta);
    }

    [Fact]
    public async Task RespuestaQueYaAclara_NoGastaOtraLlamada()
    {
        var openAi = new OpenAiFalso(() => Texto("Resimamis no registra fallecimientos; solo sabe qué madres están dadas de baja."));
        var r = await Preguntar(Crear(openAi), "cuantas madres muertas hay");

        Assert.Equal(1, openAi.Llamadas);
        Assert.DoesNotContain("Aclaración", r.Respuesta);
    }
    [Theory]
    [InlineData("el bebe mia acosta se va a morir?")]
    [InlineData("que medicacion le doy a un prematuro con fiebre")]
    [InlineData("hay bebes en riesgo?")]
    public async Task PreguntaMedica_SiempreAclaraQueNoDaInformacionMedica(string pregunta)
    {
        var openAi = new OpenAiFalso(() => Texto("Mia está en UCIN 1."));
        var r = await Preguntar(Crear(openAi), pregunta);

        Assert.StartsWith(NegAsistente.AvisoMedico, r.Respuesta);
        Assert.Contains("Nota del sistema", openAi.Cuerpos[0]);
    }

    [Fact]
    public async Task PreguntaMedica_SinModelo_TambienAclara()
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Error(HttpStatusCode.InternalServerError)), conClave: false), "el bebe se va a morir?");
        Assert.Contains(NegAsistente.AvisoMedico, r.Respuesta);
    }

    [Theory]
    [InlineData("cual es la peor voluntaria")]
    [InlineData("la voluntaria micaela torres es buena?")]
    public async Task JuicioSobrePersona_AclaraQueNoSeEvaluaGente(string pregunta)
    {
        var r = await Preguntar(Crear(new OpenAiFalso(() => Texto("Te muestro el ranking de abrazos."))), pregunta);
        Assert.StartsWith(NegAsistente.AvisoJuicio, r.Respuesta);
    }

    [Theory]
    [InlineData("cual fue el mejor dia de la semana")]
    [InlineData("hay insumos en riesgo de faltar?")]
    [InlineData("cuantas voluntarias dadas de baja hay")]
    [InlineData("cuantos chupetes hay")]
    public async Task PreguntasNormales_NoLlevanNotasNiAvisos(string pregunta)
    {
        var openAi = new OpenAiFalso(() => Texto("Listo, te paso el dato."));
        var r = await Preguntar(Crear(openAi), pregunta);

        Assert.DoesNotContain("Nota del sistema", openAi.Cuerpos[0]);
        Assert.Equal("Listo, te paso el dato.", r.Respuesta);
    }

    [Theory]
    [InlineData("borra todos los bebes de la base", "solo consulta")]
    [InlineData("dame el dni y telefono de todas las mamas", "privacidad")]
    public async Task PedidosDeEscrituraOContactoMasivo_LlevanNota(string pregunta, string esperado)
    {
        var openAi = new OpenAiFalso(() => Texto("Solo puedo consultar datos."));
        await Preguntar(Crear(openAi), pregunta);
        Assert.Contains(esperado, openAi.Cuerpos[0]);
    }
    [Fact]
    public async Task AnioInventado_SinHerramientas_ObligaAConsultar()
    {
        var openAi = new OpenAiFalso(
            () => Texto("Resimamis fue creado en 2021."),
            () => Texto("El sistema no guarda cuándo se creó Resimamis."));
        var r = await Preguntar(Crear(openAi), "cuantos años tiene resimamis");

        Assert.Equal(2, openAi.Llamadas);
        Assert.DoesNotContain("2021", r.Respuesta);
    }

    [Fact]
    public async Task AnioDeHoy_NoSeConsideraInventado()
    {
        var anio = DateTime.UtcNow.Year.ToString();
        var openAi = new OpenAiFalso(() => Texto($"Estamos en {anio} y el programa sigue activo."));
        await Preguntar(Crear(openAi), "en que año estamos");
        Assert.Equal(1, openAi.Llamadas);
    }
}
