using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

public class NegBebesTests
{
    private readonly Mock<IBebeRepositorio> _bebeRepo = new();
    private readonly Mock<IMadreRepositorio> _madreRepo = new();
    private readonly Mock<INegSalas> _salas = new();
    private readonly Mock<IGenericosRepositorio> _genericos = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();

    // ValidarCamposBebe y actualizarEstadoBebe no usan el ApplicationDbContext.
    private NegBebes Sut => new(_bebeRepo.Object, _madreRepo.Object, _salas.Object, _genericos.Object, _actividad.Object, null!);

    // registrarBebe (vía BloqueosDb) y cambiarSala sí tocan el db: InMemory.
    private NegBebes SutConDb(ApplicationDbContext db) =>
        new(_bebeRepo.Object, _madreRepo.Object, _salas.Object, _genericos.Object, _actividad.Object, db);

    private static BEBE BebeValido() => new()
    {
        nombre = "Juan",
        apellido = "Pérez",
        Sexo = "M",
        FechaNacimiento = DateTime.UtcNow.Date.AddDays(-10),
    };

    private ResultadoValidacion Validar(BEBE bebe)
    {
        var r = new ResultadoValidacion();
        Sut.ValidarCamposBebe(bebe, r);
        return r;
    }

    [Fact]
    public void ValidarCamposBebe_Valido_SinErrores()
    {
        Assert.True(Validar(BebeValido()).Exito);
    }

    [Fact]
    public void ValidarCamposBebe_Null_Falla()
    {
        var r = new ResultadoValidacion();
        Sut.ValidarCamposBebe(null!, r);
        Assert.False(r.Exito);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidarCamposBebe_SinNombre_Falla(string nombre)
    {
        var b = BebeValido();
        b.nombre = nombre;
        Assert.Contains(Validar(b).Errores, e => e.Contains("Nombre"));
    }

    [Fact]
    public void ValidarCamposBebe_NombreConDigitos_Falla()
    {
        var b = BebeValido();
        b.nombre = "Juan2";
        Assert.Contains(Validar(b).Errores, e => e.Contains("Nombre"));
    }

    [Theory]
    [InlineData("M")]
    [InlineData("f")]
    [InlineData("O")]
    public void ValidarCamposBebe_SexoValido_Ok(string sexo)
    {
        var b = BebeValido();
        b.Sexo = sexo;
        Assert.True(Validar(b).Exito);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("masculino")]
    [InlineData("")]
    public void ValidarCamposBebe_SexoInvalido_Falla(string sexo)
    {
        var b = BebeValido();
        b.Sexo = sexo;
        Assert.Contains(Validar(b).Errores, e => e.Contains("Sexo"));
    }

    [Fact]
    public void ValidarCamposBebe_DniInvalido_Falla()
    {
        var b = BebeValido();
        b.Dni = 123; // menos de 7 dígitos
        Assert.Contains(Validar(b).Errores, e => e.Contains("Dni"));
    }

    [Fact]
    public void ValidarCamposBebe_DniValido_Ok()
    {
        var b = BebeValido();
        b.Dni = 45123678;
        Assert.True(Validar(b).Exito);
    }

    [Fact]
    public void ValidarCamposBebe_FechaNacimientoFutura_Falla()
    {
        var b = BebeValido();
        b.FechaNacimiento = DateTime.UtcNow.Date.AddDays(5);
        Assert.Contains(Validar(b).Errores, e => e.Contains("FechaNacimiento"));
    }

    [Fact]
    public void ValidarCamposBebe_SinFechaNacimiento_Falla()
    {
        var b = BebeValido();
        b.FechaNacimiento = null;
        Assert.Contains(Validar(b).Errores, e => e.Contains("FechaNacimiento"));
    }

    [Fact]
    public void ValidarCamposBebe_IngresoNEOAnteriorANacimiento_Falla()
    {
        var b = BebeValido();
        b.FechaNacimiento = new DateTime(2026, 6, 10);
        b.FechaIngresoNEO = new DateTime(2026, 6, 5);
        Assert.Contains(Validar(b).Errores, e => e.Contains("FechaIngresoNEO"));
    }

    [Fact]
    public void ValidarCamposBebe_SalidaAnteriorAIngreso_Falla()
    {
        var b = BebeValido();
        b.FechaNacimiento = new DateTime(2026, 6, 1);
        b.FechaIngresoNEO = new DateTime(2026, 6, 10);
        b.FechaSalida = new DateTime(2026, 6, 5);
        Assert.Contains(Validar(b).Errores, e => e.Contains("FechaSalida"));
    }

    [Fact]
    public void ValidarCamposBebe_PesoNegativo_Falla()
    {
        var b = BebeValido();
        b.PesoNacimiento = -1.5m;
        Assert.Contains(Validar(b).Errores, e => e.Contains("PesoNacimiento"));
    }

    [Fact]
    public void ValidarCamposBebe_LocalidadInexistente_Falla()
    {
        _genericos.Setup(g => g.existeLocalidad(99)).Returns(false);
        var b = BebeValido();
        b.IdLocalidad = 99;
        Assert.Contains(Validar(b).Errores, e => e.Contains("Localidad"));
    }

    // --- actualizarEstadoBebe ---

    [Fact]
    public void ActualizarEstadoBebe_IdEstadoInvalido_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.actualizarEstadoBebe(1, 0));

    [Fact]
    public void ActualizarEstadoBebe_EstadoNoEsDeBebes_Lanza()
    {
        _bebeRepo.Setup(r => r.listarEstadosBebes())
            .Returns(new List<ESTADO> { new() { idEstado = 2, nombre = "Sin abrazar" } });
        Assert.Throws<ApplicationException>(() => Sut.actualizarEstadoBebe(1, 999));
    }

    [Fact]
    public void ActualizarEstadoBebe_BebeConFechaSalida_Lanza()
    {
        _bebeRepo.Setup(r => r.listarEstadosBebes())
            .Returns(new List<ESTADO> { new() { idEstado = 2, nombre = "Sin abrazar" } });
        _bebeRepo.Setup(r => r.consultarBebe(1)).Returns(new BEBE { ID = 1, FechaSalida = DateTime.UtcNow });
        Assert.Throws<ApplicationException>(() => Sut.actualizarEstadoBebe(1, 2));
    }

    [Fact]
    public void ActualizarEstadoBebe_Valido_CambiaYDevuelve()
    {
        _bebeRepo.Setup(r => r.listarEstadosBebes())
            .Returns(new List<ESTADO> { new() { idEstado = 2, nombre = "Sin abrazar" } });
        _bebeRepo.Setup(r => r.consultarBebe(1)).Returns(new BEBE { ID = 1, Estado = new ESTADO { nombre = "Sin abrazar" } });
        _bebeRepo.Setup(r => r.cambioEstadoBebe(It.IsAny<BEBE>(), 2)).Returns(true);

        var result = Sut.actualizarEstadoBebe(1, 2);

        Assert.NotNull(result);
        _bebeRepo.Verify(r => r.cambioEstadoBebe(It.IsAny<BEBE>(), 2), Times.Once);
    }

    // --- registrarBebe (BloqueosDb + db InMemory) ---

    [Fact]
    public void RegistrarBebe_Valido_Registra()
    {
        using var db = TestDb.Crear();
        _bebeRepo.Setup(r => r.registrarBebe(It.IsAny<BEBE>())).Returns(true);

        Assert.True(SutConDb(db).registrarBebe(BebeValido()));
        _bebeRepo.Verify(r => r.registrarBebe(It.IsAny<BEBE>()), Times.Once);
    }

    [Fact]
    public void RegistrarBebe_Invalido_Lanza()
    {
        using var db = TestDb.Crear();
        var b = BebeValido();
        b.Sexo = "X"; // inválido

        Assert.Throws<ApplicationException>(() => SutConDb(db).registrarBebe(b));
        _bebeRepo.Verify(r => r.registrarBebe(It.IsAny<BEBE>()), Times.Never);
    }

    [Fact]
    public void RegistrarBebe_MadreDadaDeBaja_Lanza()
    {
        using var db = TestDb.Crear();
        var madre = new MADRE { IdMadre = 5 };
        _madreRepo.Setup(r => r.consultarMadre(5)).Returns(madre);
        _madreRepo.Setup(r => r.estaDadaDeBaja(madre)).Returns(true);

        var b = BebeValido();
        b.IdMadre = 5;

        Assert.Throws<ApplicationException>(() => SutConDb(db).registrarBebe(b));
    }

    // --- cambiarSala (db InMemory) ---

    [Fact]
    public void CambiarSala_IdSalaInvalido_Lanza()
    {
        using var db = TestDb.Crear();
        Assert.Throws<ApplicationException>(() => SutConDb(db).cambiarSala(1, 0));
    }

    [Fact]
    public void CambiarSala_BebeInexistente_LanzaNotFound()
    {
        using var db = TestDb.Crear();
        Assert.Throws<NotFoundException>(() => SutConDb(db).cambiarSala(99, 2));
    }

    [Fact]
    public void CambiarSala_BebeEgresado_Lanza()
    {
        using var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Juan", FechaSalida = DateTime.UtcNow });
        db.SaveChanges();

        Assert.Throws<ApplicationException>(() => SutConDb(db).cambiarSala(1, 2));
    }

    [Fact]
    public void CambiarSala_Valida_ActualizaLaSala()
    {
        using var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Juan", IdSala = 1 });
        db.SaveChanges();

        var ok = SutConDb(db).cambiarSala(1, 2);

        Assert.True(ok);
        Assert.Equal(2, db.BEBE.Single(b => b.ID == 1).IdSala);
        _salas.Verify(s => s.ValidarSalaActivaParaBebe(2), Times.Once);
    }
}
