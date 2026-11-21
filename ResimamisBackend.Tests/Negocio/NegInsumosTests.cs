using Microsoft.Extensions.Configuration;
using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Tests.Negocio;

public class NegInsumosTests
{
    private readonly Mock<IInsumoRepositorio> _insumoRepo = new();
    private readonly Mock<INegProveedores> _proveedores = new();
    private readonly Mock<INegEnvioMail> _mail = new();
    private readonly Mock<IVoluntariaRepositorio> _voluntariaRepo = new();
    private readonly Mock<IConfiguration> _config = new();
    private readonly Mock<INegRegistroActividad> _actividad = new();

    private NegInsumos Sut => new(
        _insumoRepo.Object, _proveedores.Object, _mail.Object,
        _voluntariaRepo.Object, _config.Object, _actividad.Object, Mock.Of<INegNotificaciones>());

    private static INSUMO InsumoValido() => new()
    {
        nombre = "Pañales",
        stockMinimo = 5,
        stockMaximo = 100,
        stockActual = 50,
    };

    private static MOVIMIENTOSTOCK MovimientoValido() => new()
    {
        idInsumo = 1,
        cantidad = 5,
        esEntrada = "N",
    };

    // --- registrarInsumo ---

    [Fact]
    public void RegistrarInsumo_Valido_DevuelveRegistrado()
    {
        var insumo = InsumoValido();
        _insumoRepo.Setup(r => r.registrarInsumo(It.IsAny<INSUMO>())).Returns(insumo);

        var result = Sut.registrarInsumo(insumo);

        Assert.NotNull(result);
        _insumoRepo.Verify(r => r.registrarInsumo(It.IsAny<INSUMO>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarInsumo_SinNombre_Lanza(string nombre)
    {
        var i = InsumoValido();
        i.nombre = nombre;
        Assert.Throws<ApplicationException>(() => Sut.registrarInsumo(i));
    }

    [Fact]
    public void RegistrarInsumo_StockNegativo_Lanza()
    {
        var i = InsumoValido();
        i.stockActual = -1;
        Assert.Throws<ApplicationException>(() => Sut.registrarInsumo(i));
    }

    [Fact]
    public void RegistrarInsumo_MinimoMayorQueMaximo_Lanza()
    {
        var i = InsumoValido();
        i.stockMinimo = 100;
        i.stockMaximo = 10;
        Assert.Throws<ApplicationException>(() => Sut.registrarInsumo(i));
    }

    [Fact]
    public void RegistrarInsumo_ActualMayorQueMaximo_Lanza()
    {
        var i = InsumoValido();
        i.stockActual = 200;
        i.stockMaximo = 100;
        Assert.Throws<ApplicationException>(() => Sut.registrarInsumo(i));
    }

    [Fact]
    public void RegistrarInsumo_NombreMuyLargo_Lanza()
    {
        var i = InsumoValido();
        i.nombre = new string('a', 101);
        Assert.Throws<ApplicationException>(() => Sut.registrarInsumo(i));
    }

    // --- registrarMovimientoInsumos ---

    [Fact]
    public void RegistrarMovimiento_Null_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.registrarMovimientoInsumos(null!));

    [Fact]
    public void RegistrarMovimiento_SinInsumo_Lanza()
    {
        var m = MovimientoValido();
        m.idInsumo = 0;
        Assert.Throws<ApplicationException>(() => Sut.registrarMovimientoInsumos(m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(null)]
    public void RegistrarMovimiento_CantidadNoPositiva_Lanza(int? cantidad)
    {
        var m = MovimientoValido();
        m.cantidad = cantidad;
        Assert.Throws<ApplicationException>(() => Sut.registrarMovimientoInsumos(m));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("entrada")]
    [InlineData("")]
    [InlineData(null)]
    public void RegistrarMovimiento_EsEntradaInvalido_Lanza(string? esEntrada)
    {
        var m = MovimientoValido();
        m.esEntrada = esEntrada;
        Assert.Throws<ApplicationException>(() => Sut.registrarMovimientoInsumos(m));
    }

    [Theory]
    [InlineData("s", "S")]
    [InlineData(" n ", "N")]
    public void RegistrarMovimiento_NormalizaEsEntrada(string entrada, string esperado)
    {
        _proveedores.Setup(p => p.ValidarProveedorActivoParaMovimiento(It.IsAny<int?>()));
        _insumoRepo.Setup(r => r.registrarMovimientoStock(It.IsAny<MOVIMIENTOSTOCK>())).Returns(true);
        _insumoRepo.Setup(r => r.obtenerInsumosBajoStockMinimo()).Returns(new List<INSUMO>());

        var m = MovimientoValido();
        m.esEntrada = entrada;

        Sut.registrarMovimientoInsumos(m);

        Assert.Equal(esperado, m.esEntrada);
    }

    [Fact]
    public void RegistrarMovimiento_ObservacionMuyLarga_Lanza()
    {
        var m = MovimientoValido();
        m.observacion = new string('x', 501);
        Assert.Throws<ApplicationException>(() => Sut.registrarMovimientoInsumos(m));
    }

    [Fact]
    public void RegistrarMovimiento_Valido_RegistraYDevuelveTrue()
    {
        _proveedores.Setup(p => p.ValidarProveedorActivoParaMovimiento(It.IsAny<int?>()));
        _insumoRepo.Setup(r => r.registrarMovimientoStock(It.IsAny<MOVIMIENTOSTOCK>())).Returns(true);
        _insumoRepo.Setup(r => r.obtenerInsumosBajoStockMinimo()).Returns(new List<INSUMO>());

        Assert.True(Sut.registrarMovimientoInsumos(MovimientoValido()));
        _insumoRepo.Verify(r => r.registrarMovimientoStock(It.IsAny<MOVIMIENTOSTOCK>()), Times.Once);
    }

    [Fact]
    public void ObtenerMovimientoPorId_IdInvalido_Lanza() =>
        Assert.Throws<ApplicationException>(() => Sut.obtenerMovimientoPorId(0));
}
