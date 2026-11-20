using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class NegHorariosVoluntariaTests
{
    private readonly Mock<IHorarioRepositorio> _repo = new();
    private NegHorariosVoluntaria Sut => new(_repo.Object);

    private static HorarioVoluntaria HorarioValido() => new()
    {
        IdVoluntaria = 5,
        IdDia = 1,
        Turno = "Mañana",
    };

    // --- registrarHoraraioVoluntaria ---

    [Fact]
    public void RegistrarHorario_ListaNull_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.registrarHoraraioVoluntaria(null!));

    [Fact]
    public void RegistrarHorario_ListaVacia_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria>()));

    [Fact]
    public void RegistrarHorario_SinIdVoluntaria_Lanza()
    {
        var h = HorarioValido();
        h.IdVoluntaria = 0;
        Assert.Throws<ApplicationException>(() => Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria> { h }));
    }

    [Fact]
    public void RegistrarHorario_SinIdDia_Lanza()
    {
        var h = HorarioValido();
        h.IdDia = 0;
        Assert.Throws<ApplicationException>(() => Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria> { h }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarHorario_SinTurno_Lanza(string turno)
    {
        var h = HorarioValido();
        h.Turno = turno;
        Assert.Throws<ApplicationException>(() => Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria> { h }));
    }

    [Fact]
    public void RegistrarHorario_DuplicadoEnLaSolicitud_Lanza()
    {
        // Mismo IdVoluntaria + IdDia + Turno (case-insensitive) dos veces.
        var a = HorarioValido();
        var b = HorarioValido();
        b.Turno = "mañana"; // distinta capitalización, mismo turno
        Assert.Throws<ApplicationException>(() =>
            Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria> { a, b }));
    }

    [Fact]
    public void RegistrarHorario_Valido_RegistraYDevuelveHorariosDeLaVoluntaria()
    {
        var respuesta = new List<HorarioVoluntariaRespuesta>
        {
            new() { IdHorarioVoluntaria = 1, IdVoluntaria = 5, IdDia = 1, Turno = "Mañana" },
        };
        _repo.Setup(r => r.registrarHoraraioVoluntaria(It.IsAny<List<HorarioVoluntaria>>())).Returns(true);
        _repo.Setup(r => r.obtenerHorariosPorVoluntaria(5)).Returns(respuesta);

        var result = Sut.registrarHoraraioVoluntaria(new List<HorarioVoluntaria> { HorarioValido() });

        Assert.Single(result);
        Assert.Equal(5, result[0].IdVoluntaria);
        _repo.Verify(r => r.registrarHoraraioVoluntaria(It.IsAny<List<HorarioVoluntaria>>()), Times.Once);
    }

    // --- reemplazarHorarios ---

    [Fact]
    public void ReemplazarHorarios_IdVoluntariaInvalido_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.reemplazarHorarios(0, new List<HorarioVoluntaria>()));

    [Fact]
    public void ReemplazarHorarios_ForzaIdVoluntariaEnCadaHorario()
    {
        // El horario entra con IdVoluntaria = 0, pero reemplazarHorarios lo fuerza al id del path (5),
        // así que no debe fallar la validación de IdVoluntaria.
        _repo.Setup(r => r.reemplazarHorarios(5, It.IsAny<List<HorarioVoluntaria>>())).Returns(true);
        _repo.Setup(r => r.obtenerHorariosPorVoluntaria(5)).Returns(new List<HorarioVoluntariaRespuesta>());

        var h = HorarioValido();
        h.IdVoluntaria = 0;

        Sut.reemplazarHorarios(5, new List<HorarioVoluntaria> { h });

        _repo.Verify(r => r.reemplazarHorarios(5, It.Is<List<HorarioVoluntaria>>(l => l.All(x => x.IdVoluntaria == 5))), Times.Once);
    }

    [Fact]
    public void ReemplazarHorarios_ListaNull_LimpiaHorarios()
    {
        // Pasar null equivale a dejar la voluntaria sin horarios: no lanza.
        _repo.Setup(r => r.reemplazarHorarios(5, It.IsAny<List<HorarioVoluntaria>>())).Returns(true);
        _repo.Setup(r => r.obtenerHorariosPorVoluntaria(5)).Returns(new List<HorarioVoluntariaRespuesta>());

        var result = Sut.reemplazarHorarios(5, null!);

        Assert.Empty(result);
        _repo.Verify(r => r.reemplazarHorarios(5, It.IsAny<List<HorarioVoluntaria>>()), Times.Once);
    }

    // --- obtenerDias ---

    [Fact]
    public void ObtenerDias_DelegaEnRepositorio()
    {
        var dias = new List<DIA> { new() { IdDia = 1, Descripcion = "Lunes" } };
        _repo.Setup(r => r.obtenerDias()).Returns(dias);
        Assert.Same(dias, Sut.obtenerDias());
    }

    // --- obtenerHorariosPorVoluntaria ---

    [Fact]
    public void ObtenerHorariosPorVoluntaria_IdInvalido_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.obtenerHorariosPorVoluntaria(0));

    [Fact]
    public void ObtenerHorariosPorVoluntaria_IdValido_Delega()
    {
        var respuesta = new List<HorarioVoluntariaRespuesta> { new() { IdVoluntaria = 7 } };
        _repo.Setup(r => r.obtenerHorariosPorVoluntaria(7)).Returns(respuesta);
        Assert.Same(respuesta, Sut.obtenerHorariosPorVoluntaria(7));
    }

    // --- eliminarHorarioVoluntaria ---

    [Fact]
    public void EliminarHorario_IdInvalido_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.eliminarHorarioVoluntaria(0));

    [Fact]
    public void EliminarHorario_IdValido_EliminaLogicamente()
    {
        _repo.Setup(r => r.eliminarHorarioVoluntariaLogico(9)).Returns(true);
        Assert.True(Sut.eliminarHorarioVoluntaria(9));
    }
}
