using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Tests.Negocio;

public class NegSalasTests
{
    private readonly Mock<ISalaRepositorio> _repo = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();
    private NegSalas Sut => new(_repo.Object, _actividad.Object);

    private static SALA SalaValida() => new() { Nombre = "NEO A", Activa = true };

    [Fact]
    public void RegistrarSala_Valida_Ok()
    {
        _repo.Setup(r => r.registrarSala(It.IsAny<SALA>())).Returns(true);
        Assert.True(Sut.registrarSala(SalaValida()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarSala_SinNombre_Lanza(string nombre)
    {
        Assert.Throws<ApplicationException>(() => Sut.registrarSala(new SALA { Nombre = nombre }));
    }

    [Fact]
    public void RegistrarSala_NombreMuyLargo_Lanza()
    {
        Assert.Throws<ApplicationException>(() => Sut.registrarSala(new SALA { Nombre = new string('a', 101) }));
    }

    [Fact]
    public void RegistrarSala_NombreDuplicado_LanzaConflict()
    {
        _repo.Setup(r => r.existeOtraSalaConNombre(It.IsAny<string>(), It.IsAny<int?>())).Returns(true);
        Assert.Throws<ConflictException>(() => Sut.registrarSala(SalaValida()));
    }

    [Fact]
    public void ConsultarSala_Inexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.obtenerPorId(99)).Returns((SALA?)null);
        Assert.Throws<NotFoundException>(() => Sut.consultarSala(99));
    }

    [Fact]
    public void EliminarSala_ConBebesAsignados_Lanza()
    {
        _repo.Setup(r => r.tieneBebesActivosAsignados(3)).Returns(true);
        Assert.Throws<ApplicationException>(() => Sut.eliminarSala(3));
    }

    [Fact]
    public void EliminarSala_SinBebes_EliminaLogicamente()
    {
        _repo.Setup(r => r.tieneBebesActivosAsignados(3)).Returns(false);
        _repo.Setup(r => r.eliminarSalaLogica(3)).Returns(true);
        Assert.True(Sut.eliminarSala(3));
    }

    [Fact]
    public void ValidarSalaActivaParaBebe_Null_NoLanza()
    {
        Sut.ValidarSalaActivaParaBebe(null); // no debe lanzar
    }

    [Fact]
    public void ValidarSalaActivaParaBebe_Inexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.obtenerPorId(7)).Returns((SALA?)null);
        Assert.Throws<NotFoundException>(() => Sut.ValidarSalaActivaParaBebe(7));
    }

    [Fact]
    public void ValidarSalaActivaParaBebe_Inactiva_Lanza()
    {
        _repo.Setup(r => r.obtenerPorId(7)).Returns(new SALA { IdSala = 7, Nombre = "NEO X", Activa = false });
        Assert.Throws<ApplicationException>(() => Sut.ValidarSalaActivaParaBebe(7));
    }
}
