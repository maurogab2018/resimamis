using Moq;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class NegGenericosTests
{
    private readonly Mock<IGenericosRepositorio> _repo = new();
    private NegGenericos Sut => new(_repo.Object);

    [Fact]
    public void ObtenerLocalidades_DelegaEnRepositorio()
    {
        var localidades = new List<LOCALIDAD>
        {
            new() { idLocalidad = 1, nombre = "Córdoba", idProvincia = 1 },
            new() { idLocalidad = 2, nombre = "Rosario", idProvincia = 2 },
        };
        _repo.Setup(r => r.obtenerLocalidades()).Returns(localidades);

        Assert.Same(localidades, Sut.obtenerLocalidades());
        _repo.Verify(r => r.obtenerLocalidades(), Times.Once);
    }

    [Fact]
    public void ObtenerEstadosCiviles_DevuelveCatalogoDe6()
    {
        var estados = Sut.obtenerEstadosCiviles();

        Assert.Equal(6, estados.Count);
        Assert.Equal(Enumerable.Range(1, 6), estados.Select(e => e.id));
    }

    [Theory]
    [InlineData(1, "Soltera/o")]
    [InlineData(2, "Casada/o")]
    [InlineData(3, "Unión convivencial")]
    [InlineData(4, "Divorciada/o")]
    [InlineData(5, "Viuda/o")]
    [InlineData(6, "Separada/o")]
    public void ObtenerEstadosCiviles_MapeaCadaCodigo(int id, string nombre)
    {
        var estado = Sut.obtenerEstadosCiviles().Single(e => e.id == id);
        Assert.Equal(nombre, estado.nombre);
    }
}
