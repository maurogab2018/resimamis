using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

// Cubre el subconjunto de NegAsignacion que es validación + delegación en repositorios mockeados
// (y una consulta directa al db vía InMemory). Los flujos de abrazo con transacción explícita
// (iniciar/finalizar/generar/cerrar) quedan fuera: dependen de sembrar ESTADO+AMBITO y hoy
// arrastran el bug de estados "Iniciado"/"Finalizado" sin migración (ver 20-backend.md).
public class NegAsignacionTests
{
    private readonly Mock<IBebeRepositorio> _bebes = new();
    private readonly Mock<IVoluntariaRepositorio> _voluntarias = new();
    private readonly Mock<IAsignacionRepositorio> _asignaciones = new();
    private readonly Mock<IEstadoRepositorio> _estados = new();
    private readonly Mock<INegTareas> _tareas = new();
    private readonly Mock<INegUsuarios> _usuarios = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();

    private NegAsignacion SutConDb(ApplicationDbContext db) => new(
        _bebes.Object, _voluntarias.Object, _asignaciones.Object, _estados.Object,
        _tareas.Object, _usuarios.Object, db, _actividad.Object);

    private static RequestDetalleAsignacion DetalleValido() =>
        new() { idAsignacion = 1, idInsumo = 2, cantidadInsumo = 3 };

    // --- registrarDetalleAsignacion ---

    [Fact]
    public void RegistrarDetalle_ListaNull_Lanza()
    {
        using var db = TestDb.Crear();
        Assert.Throws<ApplicationException>(() => SutConDb(db).registrarDetalleAsignacion(null!));
    }

    [Fact]
    public void RegistrarDetalle_ListaVacia_Lanza()
    {
        using var db = TestDb.Crear();
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion>()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public void RegistrarDetalle_CantidadInvalida_Lanza(int? cantidad)
    {
        using var db = TestDb.Crear();
        var d = DetalleValido();
        d.cantidadInsumo = cantidad;
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion> { d }));
    }

    [Fact]
    public void RegistrarDetalle_SinIdAsignacion_Lanza()
    {
        using var db = TestDb.Crear();
        var d = DetalleValido();
        d.idAsignacion = 0;
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion> { d }));
    }

    [Fact]
    public void RegistrarDetalle_SinIdInsumo_Lanza()
    {
        using var db = TestDb.Crear();
        var d = DetalleValido();
        d.idInsumo = null;
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion> { d }));
    }

    [Fact]
    public void RegistrarDetalle_AsignacionEliminada_Lanza()
    {
        using var db = TestDb.Crear();
        _estados.Setup(e => e.ObtenerIdEstadoEliminado("Asignaciones")).Returns(99);
        _asignaciones.Setup(r => r.consultarAsignacion(1)).Returns(new ASIGNACION { idAsignacion = 1, idEstado = 99 });

        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion> { DetalleValido() }));
    }

    [Fact]
    public void RegistrarDetalle_Valido_Registra()
    {
        using var db = TestDb.Crear();
        _estados.Setup(e => e.ObtenerIdEstadoEliminado("Asignaciones")).Returns(99);
        _asignaciones.Setup(r => r.consultarAsignacion(1)).Returns(new ASIGNACION { idAsignacion = 1, idEstado = 1 });
        _asignaciones.Setup(r => r.registrarDetalleAsignacion(It.IsAny<List<RequestDetalleAsignacion>>())).Returns(true);

        Assert.True(SutConDb(db).registrarDetalleAsignacion(new List<RequestDetalleAsignacion> { DetalleValido() }));
    }

    // --- eliminarAsignacion (camino cerrado: solo delega, no toca db) ---

    [Fact]
    public void EliminarAsignacion_YaCerrada_DelegaSinLiberar()
    {
        using var db = TestDb.Crear();
        _asignaciones.Setup(r => r.consultarAsignacion(1))
            .Returns(new ASIGNACION { idAsignacion = 1, fechaHoraFin = DateTime.UtcNow });
        _asignaciones.Setup(r => r.eliminarAsignacionLogica(1)).Returns(true);

        Assert.True(SutConDb(db).eliminarAsignacion(1));
        _asignaciones.Verify(r => r.eliminarAsignacionLogica(1), Times.Once);
    }

    // --- consultas / delegaciones ---

    [Fact]
    public void ConsultarAsignacionPorId_Inexistente_LanzaNotFound()
    {
        using var db = TestDb.Crear();
        Assert.Throws<NotFoundException>(() => SutConDb(db).consultarAsignacionPorId(1));
    }

    [Fact]
    public void ListarAsignacionesHoyVoluntaria_SinDatos_DevuelveVacio()
    {
        using var db = TestDb.Crear();
        _asignaciones.Setup(r => r.listarAsignacionesHoyVoluntaria(5)).Returns(new List<ASIGNACION>());
        Assert.Empty(SutConDb(db).listarAsignacionesHoyVoluntaria(5));
    }

    [Fact]
    public void ListarAbrazosHistoricos_ConsultaElBebeYMapea()
    {
        using var db = TestDb.Crear();
        _asignaciones.Setup(r => r.listarAbrazosHistoricosPorBebe(7)).Returns(new List<ASIGNACION>());

        Assert.Empty(SutConDb(db).listarAbrazosHistoricos(7));
        _bebes.Verify(r => r.consultarBebe(7), Times.Once);
    }

    [Fact]
    public void DevolverDuracionesAbrazos_DelegaEnRepositorio()
    {
        using var db = TestDb.Crear();
        var stats = new EstadisticaDuracionesAbrazos();
        _asignaciones.Setup(r => r.devolverDuracionesAbrazos()).Returns(stats);
        Assert.Same(stats, SutConDb(db).devolverDuracionesAbrazos());
    }
}
