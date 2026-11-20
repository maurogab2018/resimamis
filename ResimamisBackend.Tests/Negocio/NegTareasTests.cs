using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class NegTareasTests
{
    private readonly Mock<ITareaRepositorio> _repo = new(MockBehavior.Strict);
    private NegTareas Sut => new(_repo.Object);

    private static TAREA TareaValida(string nombre = "Limpieza") => new() { nombre = nombre, Estado = true };

    [Fact]
    public void RegistrarTarea_Valida_LlamaAlRepositorioYTrimeaElNombre()
    {
        _repo.Setup(r => r.existeOtraTareaConNombre("Limpieza", null)).Returns(false);
        _repo.Setup(r => r.registrarTarea(It.Is<TAREA>(t => t.nombre == "Limpieza"))).Returns(true);

        var ok = Sut.registrarTarea(TareaValida("  Limpieza  "));

        Assert.True(ok);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegistrarTarea_SinNombre_Lanza(string? nombre)
    {
        Assert.Throws<ApplicationException>(() => Sut.registrarTarea(new TAREA { nombre = nombre! }));
    }

    [Fact]
    public void RegistrarTarea_NombreMuyLargo_Lanza()
    {
        var larga = new string('a', 101);
        Assert.Throws<ApplicationException>(() => Sut.registrarTarea(TareaValida(larga)));
    }

    [Fact]
    public void RegistrarTarea_NombreDuplicado_LanzaConflict()
    {
        _repo.Setup(r => r.existeOtraTareaConNombre("Limpieza", null)).Returns(true);
        Assert.Throws<ConflictException>(() => Sut.registrarTarea(TareaValida()));
    }

    [Fact]
    public void ModificarTarea_NombreDuplicadoEnOtra_LanzaConflict()
    {
        _repo.Setup(r => r.obtenerParaModificar(7)).Returns(new TAREA { idTarea = 7, nombre = "Vieja" });
        _repo.Setup(r => r.existeOtraTareaConNombre("Limpieza", 7)).Returns(true);
        Assert.Throws<ConflictException>(() => Sut.modificarTarea(7, TareaValida()));
    }

    [Fact]
    public void EliminarTarea_ConAsignacionActiva_Lanza()
    {
        _repo.Setup(r => r.tareaUnicaOcupadaHoy(5)).Returns(true);
        Assert.Throws<ApplicationException>(() => Sut.eliminarTarea(5));
    }

    [Fact]
    public void EliminarTarea_Libre_EliminaLogicamente()
    {
        _repo.Setup(r => r.tareaUnicaOcupadaHoy(5)).Returns(false);
        _repo.Setup(r => r.eliminarTareaLogica(5)).Returns(true);

        Assert.True(Sut.eliminarTarea(5));
        _repo.VerifyAll();
    }

    [Fact]
    public void ConsultarTarea_Inexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.obtenerPorId(99)).Returns((TAREA?)null);
        Assert.Throws<NotFoundException>(() => Sut.consultarTarea(99));
    }

    [Fact]
    public void ValidarDisponibleParaAsignar_TareaInactiva_Lanza()
    {
        _repo.Setup(r => r.obtenerPorId(1)).Returns(new TAREA { idTarea = 1, nombre = "X", Estado = false });
        Assert.Throws<ApplicationException>(() => Sut.ValidarTareaDisponibleParaAsignar(1));
    }

    [Fact]
    public void ValidarDisponibleParaAsignar_UnicaYaOcupada_LanzaConflict()
    {
        _repo.Setup(r => r.obtenerPorId(1)).Returns(new TAREA { idTarea = 1, nombre = "Kangaroo", Estado = true, esUnica = true });
        _repo.Setup(r => r.tareaUnicaOcupadaHoy(1)).Returns(true);
        Assert.Throws<ConflictException>(() => Sut.ValidarTareaDisponibleParaAsignar(1));
    }

    [Fact]
    public void ValidarDisponibleParaAsignar_UnicaLibre_NoLanza()
    {
        _repo.Setup(r => r.obtenerPorId(1)).Returns(new TAREA { idTarea = 1, nombre = "Kangaroo", Estado = true, esUnica = true });
        _repo.Setup(r => r.tareaUnicaOcupadaHoy(1)).Returns(false);

        Sut.ValidarTareaDisponibleParaAsignar(1); // no exception
        _repo.VerifyAll();
    }
}
