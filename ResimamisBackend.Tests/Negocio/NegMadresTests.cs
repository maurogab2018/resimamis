using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

public class NegMadresTests
{
    private readonly Mock<IMadreRepositorio> _madreRepo = new();
    private readonly Mock<INegBebes> _negBebes = new();
    private readonly Mock<IGenericosRepositorio> _genericos = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();

    // modificarMadre y consultarMadrePorDni no usan el ApplicationDbContext (solo registrarMadre
    // lo usa vía BloqueosDb), así que se puede pasar null para esos caminos.
    private NegMadres Sut => new(_madreRepo.Object, _negBebes.Object, _genericos.Object, _actividad.Object, null!);

    // registrarMadre envuelve todo en BloqueosDb.Ejecutar (abre una transacción), así que necesita
    // un db real; con InMemory el lock se saltea y la transacción es un no-op.
    private NegMadres SutConDb(ApplicationDbContext db) =>
        new(_madreRepo.Object, _negBebes.Object, _genericos.Object, _actividad.Object, db);

    private static MADRE MadreValida() => new()
    {
        IdMadre = 10,
        Nombre = "Ana",
        Apellido = "López",
        FechaNacimiento = DateTime.UtcNow.Date.AddYears(-30),
        Dni = 12345678,
        Localidad = 1,
        EstadoCivil = 1,
        CantidadHijos = 2,
        MotivoAbrazo = "Bebé prematuro",
        Celular = 1123456789L, // 10 dígitos
    };

    // --- consultarMadrePorDni ---

    [Theory]
    [InlineData(123)]       // muy corto
    [InlineData(123456789)] // muy largo
    [InlineData(0)]
    public void ConsultarMadrePorDni_DniInvalido_Lanza(int dni)
    {
        Assert.Throws<ApplicationException>(() => Sut.consultarMadrePorDni(dni));
    }

    [Fact]
    public void ConsultarMadrePorDni_NoExiste_LanzaNotFound()
    {
        _madreRepo.Setup(r => r.consultarMadrePorDni(12345678)).Returns((MADRE?)null);
        Assert.Throws<NotFoundException>(() => Sut.consultarMadrePorDni(12345678));
    }

    [Fact]
    public void ConsultarMadrePorDni_Existe_DevuelveLaMadre()
    {
        var madre = MadreValida();
        _madreRepo.Setup(r => r.consultarMadrePorDni(12345678)).Returns(madre);

        Assert.Same(madre, Sut.consultarMadrePorDni(12345678));
    }

    // --- modificarMadre: validación de ValidarCamposMadre sobre la entidad combinada ---

    private void ConMadreExistenteValida()
    {
        _madreRepo.Setup(r => r.consultarMadre(10)).Returns(MadreValida());
        _genericos.Setup(g => g.existeLocalidad(It.IsAny<int>())).Returns(true);
    }

    [Fact]
    public void ModificarMadre_EstadoCivilFueraDeRango_Falla()
    {
        ConMadreExistenteValida();
        var parcial = new MADRE { EstadoCivil = 7 }; // catálogo válido 1..6

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("EstadoCivil"));
    }

    [Fact]
    public void ModificarMadre_CelularInvalido_Falla()
    {
        ConMadreExistenteValida();
        var parcial = new MADRE { Celular = 123 }; // menos de 10 dígitos

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("Celular"));
    }

    [Fact]
    public void ModificarMadre_CantidadHijosFueraDeRango_Falla()
    {
        ConMadreExistenteValida();
        var parcial = new MADRE { CantidadHijos = 11 }; // máximo 10

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("CantidadHijos"));
    }

    [Fact]
    public void ModificarMadre_CambioValido_Exito()
    {
        ConMadreExistenteValida();
        _madreRepo.Setup(r => r.existeOtraMadreConDni(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _madreRepo.Setup(r => r.modificarMadre(It.IsAny<MADRE>(), It.IsAny<MADRE>())).Returns(MadreValida());

        var parcial = new MADRE { CantidadHijos = 3 }; // único cambio, válido

        var r = Sut.modificarMadre(parcial, 10, out var actualizada);

        Assert.True(r.Exito);
        Assert.NotNull(actualizada);
        _madreRepo.Verify(x => x.modificarMadre(It.IsAny<MADRE>(), It.IsAny<MADRE>()), Times.Once);
    }

    [Fact]
    public void ModificarMadre_MenorDe13_Falla()
    {
        // Regla de negocio: edad mínima 13 años (01-negocio.md).
        ConMadreExistenteValida();
        var parcial = new MADRE { FechaNacimiento = DateTime.UtcNow.Date.AddYears(-12) };

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("edad"));
    }

    [Fact]
    public void ModificarMadre_13OMas_Ok()
    {
        ConMadreExistenteValida();
        _madreRepo.Setup(r => r.existeOtraMadreConDni(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _madreRepo.Setup(r => r.modificarMadre(It.IsAny<MADRE>(), It.IsAny<MADRE>())).Returns(MadreValida());

        var parcial = new MADRE { FechaNacimiento = DateTime.UtcNow.Date.AddYears(-13).AddDays(-1) };

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.True(r.Exito);
    }

    [Fact]
    public void ModificarMadre_MotivoAbrazoDe251_Falla()
    {
        // Spec: MotivoAbrazo máx 250 caracteres.
        ConMadreExistenteValida();
        var parcial = new MADRE { MotivoAbrazo = new string('x', 251) };

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("MotivoAbrazo"));
    }

    [Fact]
    public void ModificarMadre_CelularDe16Digitos_Falla()
    {
        // Spec: celular 10-15 dígitos.
        ConMadreExistenteValida();
        var parcial = new MADRE { Celular = 1234567890123456L }; // 16 dígitos

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("Celular"));
    }

    [Fact]
    public void ModificarMadre_CelularDe15Digitos_Ok()
    {
        // Antes el backend topaba en 13 dígitos; ahora acepta hasta 15 (alineado a la spec).
        ConMadreExistenteValida();
        _madreRepo.Setup(r => r.existeOtraMadreConDni(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _madreRepo.Setup(r => r.modificarMadre(It.IsAny<MADRE>(), It.IsAny<MADRE>())).Returns(MadreValida());

        var parcial = new MADRE { Celular = 123456789012345L }; // 15 dígitos

        var r = Sut.modificarMadre(parcial, 10, out _);

        Assert.True(r.Exito);
    }

    // --- registrarMadre (BloqueosDb + db InMemory) ---

    private static MADRE MadreConBebe()
    {
        var m = MadreValida();
        m.Bebe = new List<BEBE>
        {
            new()
            {
                nombre = "Juan",
                apellido = "López",
                Sexo = "M",
                FechaNacimiento = DateTime.UtcNow.Date.AddDays(-5),
            },
        };
        return m;
    }

    [Fact]
    public void RegistrarMadre_Valida_RegistraYExito()
    {
        using var db = TestDb.Crear();
        _genericos.Setup(g => g.existeLocalidad(It.IsAny<int>())).Returns(true);
        _madreRepo.Setup(r => r.registrarMadre(It.IsAny<MADRE>())).Returns(true);

        var r = SutConDb(db).registrarMadre(MadreConBebe());

        Assert.True(r.Exito);
        _madreRepo.Verify(x => x.registrarMadre(It.IsAny<MADRE>()), Times.Once);
    }

    [Fact]
    public void RegistrarMadre_SinBebe_FallaYNoLlamaAlRepositorio()
    {
        using var db = TestDb.Crear();
        _genericos.Setup(g => g.existeLocalidad(It.IsAny<int>())).Returns(true);

        var madre = MadreValida(); // sin Bebe
        var r = SutConDb(db).registrarMadre(madre);

        Assert.False(r.Exito);
        _madreRepo.Verify(x => x.registrarMadre(It.IsAny<MADRE>()), Times.Never);
    }

    [Fact]
    public void RegistrarMadre_RepositorioDevuelveFalse_AgregaError()
    {
        using var db = TestDb.Crear();
        _genericos.Setup(g => g.existeLocalidad(It.IsAny<int>())).Returns(true);
        _madreRepo.Setup(r => r.registrarMadre(It.IsAny<MADRE>())).Returns(false);

        var r = SutConDb(db).registrarMadre(MadreConBebe());

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("Error al registrar la madre"));
    }
}
