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
        INegDashboard? dashboard = null, INegMadres? madres = null, INegBebes? bebes = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Asistente:Enabled"] = "true",
            ["Asistente:ApiKey"] = "sk-test",
            ["Asistente:MaxPreguntasDia"] = "0",
        }).Build();
        return new NegAsistente(config, Mock.Of<INegUsuarios>(), dashboard ?? Mock.Of<INegDashboard>(), bebes ?? Mock.Of<INegBebes>(),
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
    [Fact]
    public async Task LinkInventado_SeExigeConsultarYSeUsaElNombreReal()
    {
        var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Lucas", apellido = "González" });
        db.SaveChanges();
        var openAi = new OpenAiGuion(
            () => Texto("Ingresaron: [Bebé 1](bebe:1)."),
            () => Texto("Ingresó [Lucas González](bebe:1)."));

        var r = await Crear(openAi, db).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "como se llaman los bebes que ingresaron" });

        Assert.Equal(2, openAi.Cuerpos.Count);
        Assert.Contains("[Lucas González](bebe:1)", r.Respuesta);
        Assert.DoesNotContain("Bebé 1", r.Respuesta);
    }

    [Fact]
    public async Task LinkInventado_SiInsiste_SeSacaElLinkYSeMarca()
    {
        var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Lucas", apellido = "González" });
        db.SaveChanges();
        var openAi = new OpenAiGuion(() => Texto("Ingresaron: [Bebé 1](bebe:1)."));

        var r = await Crear(openAi, db).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "como se llaman los bebes que ingresaron" });

        Assert.DoesNotContain("(bebe:1)", r.Respuesta);
        Assert.Contains("Bebé 1 (sin verificar)", r.Respuesta);
    }

    [Theory]
    [InlineData("[Lucas](bebe:1)")]
    [InlineData("[Lucas González](bebe:1)")]
    [InlineData("[lucas gonzalez](bebe:1)")]
    public async Task LinkCorrecto_NoGastaOtraLlamada(string link)
    {
        var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Lucas", apellido = "González" });
        db.SaveChanges();
        var openAi = new OpenAiGuion(() => Texto($"El bebé es {link}."));

        var r = await Crear(openAi, db).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "quien es lucas" });

        Assert.Single(openAi.Cuerpos);
        Assert.Contains(link, r.Respuesta);
    }

    [Fact]
    public async Task PedidoDeBorrar_AclaraQueSoloConsulta()
    {
        var r = await Crear(new OpenAiGuion(() => Texto("Te muestro los bebés activos.")))
            .Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "borra todos los bebes de la base" });
        Assert.StartsWith(NegAsistente.AvisoSoloConsulta, r.Respuesta);
    }

    [Fact]
    public async Task GuiaOperativa_ComoDarDeAltaUnaMama_ExplicaLaPantalla()
    {
        var openAi = new OpenAiGuion(() => Herramienta("guia_operativa", "{\"tema\":\"madres\"}"), () => Texto("listo"));
        await Crear(openAi).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "como doy de alta una mama nueva" });
        Assert.Contains("Nueva madre", ResultadoHerramienta(openAi));
    }

    [Fact]
    public async Task BebesIngresados_TraeLosNombresReales()
    {
        var db = TestDb.Crear();
        var hoy = DateTime.Today;
        db.BEBE.AddRange(
            new BEBE { ID = 1, nombre = "Lucas", apellido = "Gómez", FechaIngresoNEO = hoy.AddDays(-1) },
            new BEBE { ID = 2, nombre = "Sol", apellido = "Ríos", FechaIngresoNEO = hoy.AddDays(-200), FechaSalida = hoy.AddDays(-2) });
        db.SaveChanges();
        var openAi = new OpenAiGuion(() => Herramienta("bebes_ingresados_egresados", "{\"periodo\":\"ultimos_7\"}"), () => Texto("listo"));

        await Crear(openAi, db).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "que bebes ingresaron esta semana" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.Contains("\"totalIngresados\":1", resultado);
        Assert.Contains("Lucas G", resultado);
        Assert.Contains("\"totalEgresados\":1", resultado);
    }

    [Fact]
    public void AbrazosDeUnaVoluntaria_ElTotalNoSeCortaEn50()
    {
        var db = TestDb.Crear();
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 9, Nombre = "Paula", Apellido = "Martínez", Mail = "p@x.com" });
        var inicio = DateTime.UtcNow.AddDays(-100);
        for (var i = 0; i < 70; i++)
            db.ASIGNACION.Add(new ASIGNACION
            {
                idVoluntaria = 9, idBebe = 1, idEstado = 1,
                fechaHoraAsignacion = inicio.AddHours(i), fechaHoraInicio = inicio.AddHours(i), fechaHoraFin = inicio.AddHours(i).AddMinutes(30)
            });
        db.SaveChanges();
        var estados = new Mock<IEstadoRepositorio>();
        estados.Setup(e => e.ObtenerIdEstadoEliminado(It.IsAny<string>())).Returns(999);
        var repo = new DashboardRepositorio(db, estados.Object, Mock.Of<IBebeRepositorio>());

        var r = repo.ObtenerAbrazosVoluntaria(9, null, null, null);

        Assert.Equal(70, r.TotalAbrazos);
        Assert.Equal(70, r.AbrazosFinalizados);
        Assert.True(r.Abrazos.Count <= 50); // el detalle sigue limitado a los 50 más recientes
    }
    [Fact]
    public async Task AbrazosDeUnBebe_TraeLosMinutosTotalesDeEseBebe()
    {
        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerAbrazosBebeHistorial(31, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(new AbrazosBebeDashboardRespuesta
            {
                IdBebe = 31, NombreBebe = "Simón", ApellidoBebe = "Gómez", TotalAbrazos = 3, AbrazosFinalizados = 2,
                Abrazos = new() { new() { DuracionMinutos = 30 }, new() { DuracionMinutos = 45.4 }, new() { DuracionMinutos = null } }
            });
        var openAi = new OpenAiGuion(() => Herramienta("abrazos_bebe", "{\"id_bebe\":31}"), () => Texto("listo"));

        await Crear(openAi, dashboard: dashboard.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "cuantos minutos en total?" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.Contains("\"minutosTotalesDeAbrazo\":75", resultado);
        Assert.Contains("\"promedioMinutosPorAbrazo\":37.7", resultado);
    }
    [Fact]
    public async Task DuracionAbrazos_ConUnBebe_DevuelveLosMinutosDeEseBebe()
    {
        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerAbrazosBebeHistorial(31, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(new AbrazosBebeDashboardRespuesta { IdBebe = 31, AbrazosFinalizados = 1, Abrazos = new() { new() { DuracionMinutos = 40 } } });
        var openAi = new OpenAiGuion(() => Herramienta("duracion_abrazos", "{\"id_bebe\":31}"), () => Texto("listo"));

        await Crear(openAi, dashboard: dashboard.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "cuantos minutos en total?" });

        Assert.Contains("\"minutosTotalesDeAbrazo\":40", ResultadoHerramienta(openAi));
        dashboard.Verify(d => d.ObtenerDuracionAbrazos(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
    }
    [Fact]
    public async Task TareasDeHoy_NoSeRechazanComoTemaAjeno()
    {
        var openAi = new OpenAiGuion(
            () => Texto("Solo puedo ayudarte con datos de Resimamis."),
            () => Herramienta("tareas_periodo", "{\"periodo\":\"hoy\"}"),
            () => Texto("Hoy no hay tareas asignadas."));

        var r = await Crear(openAi).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "cuantas tareas hay hoy" });

        Assert.Equal("Hoy no hay tareas asignadas.", r.Respuesta);
        Assert.Contains("tareas_periodo", r.HerramientasUsadas);
    }

    [Theory]
    [InlineData("cual es la capital de francia")]
    [InlineData("contame un chiste")]
    public async Task TemaAjenoDeVerdad_SeAceptaElRechazo(string pregunta)
    {
        var openAi = new OpenAiGuion(() => Texto("Solo puedo ayudarte con datos de Resimamis."));
        var r = await Crear(openAi).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = pregunta });

        Assert.Single(openAi.Cuerpos);
        Assert.Equal("Solo puedo ayudarte con datos de Resimamis.", r.Respuesta);
    }

    [Fact]
    public async Task TareasPeriodo_CuentaTerminadasEnCursoYPendientes()
    {
        var db = TestDb.Crear();
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 1, Nombre = "Paula", Apellido = "Martínez", Mail = "p@x.com" });
        var ahora = DateTime.UtcNow;
        db.ASIGNACION.AddRange(
            new ASIGNACION { idTarea = 1, idVoluntaria = 1, fechaHoraAsignacion = ahora, fechaHoraInicio = ahora, fechaHoraFin = ahora.AddMinutes(20) },
            new ASIGNACION { idTarea = 1, idVoluntaria = 1, fechaHoraAsignacion = ahora, fechaHoraInicio = ahora },
            new ASIGNACION { idTarea = 2, idVoluntaria = 1, fechaHoraAsignacion = ahora },
            new ASIGNACION { idBebe = 5, idVoluntaria = 1, fechaHoraAsignacion = ahora });
        db.SaveChanges();
        var openAi = new OpenAiGuion(() => Herramienta("tareas_periodo", "{\"periodo\":\"hoy\"}"), () => Texto("listo"));

        await Crear(openAi, db).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "cuantas tareas hay hoy" });

        var resultado = ResultadoHerramienta(openAi);
        Assert.Contains("\"totalTareasAsignadas\":3", resultado);
        Assert.Contains("\"terminadas\":1", resultado);
        Assert.Contains("\"enCurso\":1", resultado);
        Assert.Contains("\"pendientes\":1", resultado);
    }
    [Fact]
    public async Task AbrazosBebe_ElUltimoAbrazoRealNoEsUnCierreAutomatico()
    {
        var ayer = DateTime.UtcNow.AddDays(-1);
        var hace3 = DateTime.UtcNow.AddDays(-3);
        var dashboard = new Mock<INegDashboard>();
        dashboard.Setup(d => d.ObtenerAbrazosBebeHistorial(16, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(new AbrazosBebeDashboardRespuesta
            {
                IdBebe = 16,
                Abrazos = new()
                {
                    new() { NombreVoluntaria = "Soledad Ibañez", FechaHoraAsignacion = ayer, FechaHoraInicio = ayer, FechaHoraFin = ayer, CierreAutomatico = true },
                    new() { NombreVoluntaria = "Paula Martinez", FechaHoraAsignacion = hace3, FechaHoraInicio = hace3, FechaHoraFin = hace3.AddMinutes(40), DuracionMinutos = 40 },
                    new() { NombreVoluntaria = "Soledad Ibañez", FechaHoraAsignacion = hace3.AddDays(-1), FechaHoraInicio = hace3.AddDays(-1), FechaHoraFin = hace3.AddDays(-1).AddMinutes(30), DuracionMinutos = 30 },
                    new() { NombreVoluntaria = "Soledad Ibañez", FechaHoraAsignacion = hace3.AddDays(-2), FechaHoraInicio = hace3.AddDays(-2), FechaHoraFin = hace3.AddDays(-2).AddMinutes(30), DuracionMinutos = 30 },
                }
            });
        var openAi = new OpenAiGuion(() => Herramienta("abrazos_bebe", "{\"id_bebe\":16}"), () => Texto("listo"));

        await Crear(openAi, dashboard: dashboard.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "quien abrazo a mia la ultima vez" });

        var resultado = ResultadoHerramienta(openAi);
        var ultimo = resultado[(resultado.IndexOf("\"ultimoAbrazoReal\"") + 1)..];
        Assert.StartsWith("ultimoAbrazoReal\":{", ultimo);
        Assert.Contains("Paula Martinez", ultimo[..200]);
        Assert.Contains("{\"voluntaria\":\"Soledad Iba", resultado[resultado.IndexOf("\"porVoluntaria\"")..]);
        Assert.Contains("\"abrazos\":2", resultado[resultado.IndexOf("\"porVoluntaria\"")..]);
    }

    [Fact]
    public async Task ListarBebes_LaSalaDeUnBebeYPesoAlNacer()
    {
        var neoA = new SALA { IdSala = 1, Nombre = "NEO A" };
        var ucin = new SALA { IdSala = 2, Nombre = "UCIN 1" };
        var bebes = new Mock<INegBebes>();
        bebes.Setup(b => b.listarBebes()).Returns(new List<BEBE>
        {
            new() { ID = 16, nombre = "Mia", apellido = "Acosta", Sala = ucin, PesoNacimiento = 1320 },
            new() { ID = 17, nombre = "Leo", apellido = "Paz", Sala = ucin, PesoNacimiento = 2400 },
            new() { ID = 18, nombre = "Ana", apellido = "Gil", Sala = neoA, PesoNacimiento = 1100 },
        });
        var openAi = new OpenAiGuion(
            () => Herramienta("listar_bebes", "{\"filtro_sala\":\"mia\"}"),
            () => Herramienta("listar_bebes", "{\"peso_nacimiento_menor_a\":1500}"),
            () => Texto("listo"));

        await Crear(openAi, bebes: bebes.Object).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "que bebes estan en la sala de mia" });

        var salaDeMia = System.Text.RegularExpressions.Regex.Unescape(openAi.Cuerpos[1]);
        Assert.Contains("\"total\":2", salaDeMia[salaDeMia.LastIndexOf("\"role\":\"tool\"")..]);
        var livianos = System.Text.RegularExpressions.Regex.Unescape(openAi.Cuerpos[2]);
        var ultimoTool = livianos[livianos.LastIndexOf("\"role\":\"tool\"")..];
        Assert.Contains("\"total\":2", ultimoTool);
        Assert.DoesNotContain("Leo", ultimoTool);
    }

    [Fact]
    public async Task Prompt_TraeElCalendarioConDiasDeLaSemana()
    {
        var openAi = new OpenAiGuion(() => Texto("listo"));
        await Crear(openAi).Preguntar(Dni, new AsistentePreguntaRequest { Pregunta = "a que hora ficho paula el lunes" });

        var prompt = System.Text.RegularExpressions.Regex.Unescape(openAi.Cuerpos[0]);
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time")));
        var dias = new[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
        var ayer = hoy.AddDays(-1);
        Assert.Contains($"{dias[(int)ayer.DayOfWeek]} {ayer:yyyy-MM-dd}", prompt);
        Assert.Contains("Calendario de los últimos días", prompt);
    }
}
