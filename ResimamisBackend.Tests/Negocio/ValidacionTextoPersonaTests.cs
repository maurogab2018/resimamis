using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class ValidacionTextoPersonaTests
{
    [Theory]
    [InlineData("María")]
    [InlineData("José Pérez")]
    [InlineData("Ana Belén Juárez")]
    [InlineData("D'Angelo")]
    [InlineData("O'Connor")]
    [InlineData("Pérez-García")]
    [InlineData("Ñandú")]
    [InlineData("  Juan  ")] // se hace trim antes de validar
    public void EsNombreApellidoValido_NombresValidos_EsTrue(string valor)
    {
        Assert.True(ValidacionTextoPersona.EsNombreApellidoValido(valor));
    }

    [Theory]
    [InlineData("Juan123")]
    [InlineData("123")]
    [InlineData("Juan@")]
    [InlineData("-Juan")]   // debe empezar con letra
    [InlineData("'Juan")]   // debe empezar con letra
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EsNombreApellidoValido_Invalidos_EsFalse(string? valor)
    {
        Assert.False(ValidacionTextoPersona.EsNombreApellidoValido(valor));
    }

    [Theory]
    [InlineData(1234567)]   // 7 dígitos
    [InlineData(12345678)]  // 8 dígitos
    public void EsDniValido_7u8Digitos_EsTrue(int dni)
    {
        Assert.True(ValidacionTextoPersona.EsDniValido(dni));
    }

    [Theory]
    [InlineData(123456)]     // 6 dígitos
    [InlineData(123456789)]  // 9 dígitos
    [InlineData(0)]
    [InlineData(-12345678)]
    public void EsDniValido_FueraDeRango_EsFalse(int dni)
    {
        Assert.False(ValidacionTextoPersona.EsDniValido(dni));
    }

    [Fact]
    public void Normalizar_RecortaEspacios()
    {
        Assert.Equal("Juan", ValidacionTextoPersona.Normalizar("  Juan  "));
    }

    [Fact]
    public void Normalizar_Null_DevuelveNull()
    {
        Assert.Null(ValidacionTextoPersona.Normalizar(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_VacioOEspacios_DevuelveElValorSinTocar(string valor)
    {
        // Contrato actual: si es null-or-whitespace devuelve el valor tal cual (no recorta).
        Assert.Equal(valor, ValidacionTextoPersona.Normalizar(valor));
    }
}
