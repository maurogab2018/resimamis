using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Tests.Negocio;

public class NegProveedoresTests
{
    private readonly Mock<IProveedorRepositorio> _repo = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();
    private NegProveedores Sut => new(_repo.Object, _actividad.Object);

    private static PROVEEDOR ProveedorValido() => new() { nombre = "Farmacia Central", Activa = true };

    [Fact]
    public void RegistrarProveedor_Valido_Ok()
    {
        _repo.Setup(r => r.registrarProveedor(It.IsAny<PROVEEDOR>())).Returns(true);
        Assert.True(Sut.registrarProveedor(ProveedorValido()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarProveedor_SinNombre_Lanza(string nombre)
    {
        Assert.Throws<ApplicationException>(() => Sut.registrarProveedor(new PROVEEDOR { nombre = nombre }));
    }

    [Fact]
    public void RegistrarProveedor_NombreMuyLargo_Lanza()
    {
        Assert.Throws<ApplicationException>(() => Sut.registrarProveedor(new PROVEEDOR { nombre = new string('a', 201) }));
    }

    [Fact]
    public void RegistrarProveedor_DescripcionMuyLarga_Lanza()
    {
        var p = ProveedorValido();
        p.descripcion = new string('x', 501);
        Assert.Throws<ApplicationException>(() => Sut.registrarProveedor(p));
    }

    [Fact]
    public void RegistrarProveedor_NombreDuplicado_LanzaConflict()
    {
        _repo.Setup(r => r.existeOtroProveedorConNombre(It.IsAny<string>(), It.IsAny<int?>())).Returns(true);
        Assert.Throws<ConflictException>(() => Sut.registrarProveedor(ProveedorValido()));
    }

    [Fact]
    public void ConsultarProveedor_Inexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.obtenerPorId(99)).Returns((PROVEEDOR?)null);
        Assert.Throws<NotFoundException>(() => Sut.consultarProveedor(99));
    }

    [Fact]
    public void ValidarProveedorActivoParaMovimiento_Null_NoLanza()
    {
        Sut.ValidarProveedorActivoParaMovimiento(null); // no debe lanzar
    }

    [Fact]
    public void ValidarProveedorActivoParaMovimiento_Inexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.obtenerPorId(5)).Returns((PROVEEDOR?)null);
        Assert.Throws<NotFoundException>(() => Sut.ValidarProveedorActivoParaMovimiento(5));
    }

    [Fact]
    public void ValidarProveedorActivoParaMovimiento_Inactivo_Lanza()
    {
        _repo.Setup(r => r.obtenerPorId(5)).Returns(new PROVEEDOR { idProveedor = 5, nombre = "X", Activa = false });
        Assert.Throws<ApplicationException>(() => Sut.ValidarProveedorActivoParaMovimiento(5));
    }

    [Fact]
    public void ValidarProveedorActivoParaMovimiento_Activo_NoLanza()
    {
        _repo.Setup(r => r.obtenerPorId(5)).Returns(new PROVEEDOR { idProveedor = 5, nombre = "X", Activa = true });
        Sut.ValidarProveedorActivoParaMovimiento(5); // no debe lanzar
    }
}
