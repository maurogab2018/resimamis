using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class RolesVoluntariaTests
{
    [Fact]
    public void EsCoordinadora_PorIdRolCoordinadora_EsTrue()
    {
        Assert.True(RolesVoluntaria.EsCoordinadora(RolesVoluntaria.IdCoordinadora, null));
    }

    [Fact]
    public void EsCoordinadora_PorIdRolVoluntaria_EsFalse()
    {
        Assert.False(RolesVoluntaria.EsCoordinadora(RolesVoluntaria.IdVoluntaria, null));
    }

    [Theory]
    [InlineData("Coordinadora")]
    [InlineData("Administrativa")]
    [InlineData("Administrador")]
    [InlineData("Admin")]
    public void EsCoordinadora_PorNombreDeRolGestion_EsTrue(string nombreRol)
    {
        Assert.True(RolesVoluntaria.EsCoordinadora(null, nombreRol));
    }

    [Theory]
    [InlineData("coordinadora")]
    [InlineData("ADMINISTRATIVA")]
    [InlineData("aDmIn")]
    public void EsCoordinadora_NombreInsensibleAMayusculas_EsTrue(string nombreRol)
    {
        Assert.True(RolesVoluntaria.EsCoordinadora(null, nombreRol));
    }

    [Fact]
    public void EsCoordinadora_NombreCoordinadoraConIdNoCoordinadora_EsTrue()
    {
        // El nombre alcanza aunque el idRol no sea 3.
        Assert.True(RolesVoluntaria.EsCoordinadora(RolesVoluntaria.IdVoluntaria, "Coordinadora"));
    }

    [Theory]
    [InlineData("Voluntaria")]
    [InlineData("Enfermera")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EsCoordinadora_SinIdNiNombreValido_EsFalse(string? nombreRol)
    {
        Assert.False(RolesVoluntaria.EsCoordinadora(null, nombreRol));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(RolesVoluntaria.IdVoluntaria, true)]
    [InlineData(RolesVoluntaria.IdCoordinadora, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(99, false)]
    public void EsRolPermitido_SoloVoluntariaOCoordinadoraONull(int? idRol, bool esperado)
    {
        Assert.Equal(esperado, RolesVoluntaria.EsRolPermitido(idRol));
    }
}
