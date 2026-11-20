using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class NegVisitasTests
{
    private readonly Mock<IVisitaRepositorio> _visitaRepo = new();
    private readonly Mock<IBebeRepositorio> _bebeRepo = new();
    private NegVisitas Sut => new(_visitaRepo.Object, _bebeRepo.Object);

    private static VISITA VisitaValida() => new()
    {
        idBebe = 1,
        nombreVisitante = "María Gómez",
        familiar = "Madre",
        fechaHoraVisita = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc),
    };

    private static BEBE BebeValido() => new()
    {
        ID = 1,
        FechaSalida = null,
        Estado = new ESTADO { nombre = "Sin abrazar" },
    };

    private void ConBebeValido() => _bebeRepo.Setup(r => r.consultarBebe(1)).Returns(BebeValido());

    [Fact]
    public void RegistrarVisita_Valida_ActivaYFechaRegistro()
    {
        ConBebeValido();
        _visitaRepo.Setup(r => r.registrarVisita(It.IsAny<VISITA>())).Returns(true);

        var result = Sut.registrarVisita(VisitaValida());

        Assert.True(result.Activa);
        Assert.NotEqual(default, result.fechaRegistro);
        _visitaRepo.Verify(r => r.registrarVisita(It.IsAny<VISITA>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarVisita_SinNombreVisitante_Lanza(string nombre)
    {
        var v = VisitaValida();
        v.nombreVisitante = nombre;
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_NombreConDigitos_Lanza()
    {
        var v = VisitaValida();
        v.nombreVisitante = "Juan123";
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_SinFamiliar_Lanza()
    {
        var v = VisitaValida();
        v.familiar = "";
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_SinFecha_Lanza()
    {
        var v = VisitaValida();
        v.fechaHoraVisita = default;
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_DocumentoFueraDeRango_Lanza()
    {
        var v = VisitaValida();
        v.documentoVisitante = 12345; // 5 dígitos
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_TelefonoFueraDeRango_Lanza()
    {
        var v = VisitaValida();
        v.telefonoVisitante = 123; // muy corto
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_DocumentoYTelefonoValidos_Ok()
    {
        ConBebeValido();
        _visitaRepo.Setup(r => r.registrarVisita(It.IsAny<VISITA>())).Returns(true);

        var v = VisitaValida();
        v.documentoVisitante = 12345678;   // 8 dígitos
        v.telefonoVisitante = 1123456789;  // 10 dígitos

        var result = Sut.registrarVisita(v);
        Assert.True(result.Activa);
    }

    [Fact]
    public void RegistrarVisita_ObservacionMuyLarga_Lanza()
    {
        var v = VisitaValida();
        v.observacion = new string('x', 501);
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(v));
    }

    [Fact]
    public void RegistrarVisita_BebeConFechaSalida_Lanza()
    {
        _bebeRepo.Setup(r => r.consultarBebe(1)).Returns(new BEBE { ID = 1, FechaSalida = DateTime.UtcNow });
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(VisitaValida()));
    }

    [Fact]
    public void RegistrarVisita_BebeEliminado_Lanza()
    {
        _bebeRepo.Setup(r => r.consultarBebe(1))
            .Returns(new BEBE { ID = 1, Estado = new ESTADO { nombre = "Eliminado" } });
        Assert.Throws<ApplicationException>(() => Sut.registrarVisita(VisitaValida()));
    }

    [Fact]
    public void RegistrarVisita_BebeInexistente_PropagaNotFound()
    {
        _bebeRepo.Setup(r => r.consultarBebe(1)).Throws(new NotFoundException("Bebé no encontrado con ese Id."));
        Assert.Throws<NotFoundException>(() => Sut.registrarVisita(VisitaValida()));
    }
}
