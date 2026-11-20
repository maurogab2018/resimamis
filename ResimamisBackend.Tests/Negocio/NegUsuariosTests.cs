using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using ResimamisBackend.Tests.Infra;

namespace ResimamisBackend.Tests.Negocio;

public class NegUsuariosTests
{
    private readonly Mock<IEstadoRepositorio> _estados = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IVoluntariaRepositorio> _voluntarias = new();
    private readonly Mock<INegEnvioMail> _mail = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();

    private NegUsuarios SutConDb(ApplicationDbContext db) =>
        new(db, _estados.Object, _usuarios.Object, _voluntarias.Object, _mail.Object, _actividad.Object);

    private static USUARIO UsuarioOperativo(int dni, int idVoluntaria, string contrasenaHash) => new()
    {
        IdUsuario = 1,
        Dni = dni,
        IdVoluntaria = idVoluntaria,
        Contrasena = contrasenaHash,
        Estado = null, // sin estado "Eliminado" ⇒ operativo
    };

    private static USUARIO UsuarioEliminado(int dni) => new()
    {
        IdUsuario = 2,
        Dni = dni,
        Estado = new ESTADO { nombre = "Eliminado", ambito = new AMBITO { nombre = "Usuarios" } },
    };

    // --- EsSesionOperativaPorDni ---

    [Fact]
    public void EsSesionOperativa_DniInvalido_False()
    {
        using var db = TestDb.Crear();
        Assert.False(SutConDb(db).EsSesionOperativaPorDni(0));
    }

    [Fact]
    public void EsSesionOperativa_UsuarioInexistente_False()
    {
        using var db = TestDb.Crear();
        _usuarios.Setup(r => r.ObtenerPorDni(123, true)).Returns((USUARIO?)null);
        Assert.False(SutConDb(db).EsSesionOperativaPorDni(123));
    }

    [Fact]
    public void EsSesionOperativa_UsuarioOperativo_True()
    {
        using var db = TestDb.Crear();
        _usuarios.Setup(r => r.ObtenerPorDni(123, true)).Returns(UsuarioOperativo(123, 1, "hash"));
        Assert.True(SutConDb(db).EsSesionOperativaPorDni(123));
    }

    [Fact]
    public void EsSesionOperativa_UsuarioEliminado_False()
    {
        using var db = TestDb.Crear();
        _usuarios.Setup(r => r.ObtenerPorDni(123, true)).Returns(UsuarioEliminado(123));
        Assert.False(SutConDb(db).EsSesionOperativaPorDni(123));
    }

    // --- Loguear ---

    [Fact]
    public void Loguear_Null_Lanza()
    {
        using var db = TestDb.Crear();
        Assert.Throws<ApplicationException>(() => SutConDb(db).Loguear(null!));
    }

    [Fact]
    public void Loguear_SinContrasena_Lanza()
    {
        using var db = TestDb.Crear();
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).Loguear(new RequestLogin { Dni = 123, Contrasena = "" }));
    }

    [Fact]
    public void Loguear_UsuarioInexistente_Lanza()
    {
        using var db = TestDb.Crear();
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns((USUARIO?)null);
        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).Loguear(new RequestLogin { Dni = 123, Contrasena = "secreta123" }));
    }

    [Fact]
    public void Loguear_ContrasenaIncorrecta_Lanza()
    {
        using var db = TestDb.Crear();
        var hash = BCrypt.Net.BCrypt.HashPassword("la-correcta");
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns(UsuarioOperativo(123, 1, hash));

        Assert.Throws<ApplicationException>(() =>
            SutConDb(db).Loguear(new RequestLogin { Dni = 123, Contrasena = "la-incorrecta" }));
    }

    [Fact]
    public void Loguear_Exito_DevuelveTokenYVoluntaria()
    {
        using var db = TestDb.Crear();
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 7, Nombre = "Ana", Apellido = "López", Mail = "a@b.com" });
        db.SaveChanges();

        var hash = BCrypt.Net.BCrypt.HashPassword("secreta123");
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns(UsuarioOperativo(123, 7, hash));

        var r = SutConDb(db).Loguear(new RequestLogin { Dni = 123, Contrasena = "secreta123" });

        Assert.Equal("Exito", r.Resultado);
        Assert.False(string.IsNullOrWhiteSpace(r.Token));
        Assert.Equal(7, r.Voluntaria.IdVoluntaria);
    }

    // --- EsCoordinadoraPorDni ---

    [Fact]
    public void EsCoordinadora_UsuarioInexistente_False()
    {
        using var db = TestDb.Crear();
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns((USUARIO?)null);
        Assert.False(SutConDb(db).EsCoordinadoraPorDni(123));
    }

    [Fact]
    public void EsCoordinadora_VoluntariaConRol3_True()
    {
        using var db = TestDb.Crear();
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 7, Nombre = "Co", Apellido = "Ord", Mail = "c@o.com", IdRol = 3 });
        db.SaveChanges();
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns(UsuarioOperativo(123, 7, "hash"));

        Assert.True(SutConDb(db).EsCoordinadoraPorDni(123));
    }

    [Fact]
    public void EsCoordinadora_VoluntariaSinRolCoordinadora_False()
    {
        using var db = TestDb.Crear();
        db.VOLUNTARIA.Add(new VOLUNTARIA { IdVoluntaria = 7, Nombre = "Vol", Apellido = "Unt", Mail = "v@u.com", IdRol = 1 });
        db.SaveChanges();
        _usuarios.Setup(r => r.ObtenerPorDni(123, false)).Returns(UsuarioOperativo(123, 7, "hash"));

        Assert.False(SutConDb(db).EsCoordinadoraPorDni(123));
    }
}
