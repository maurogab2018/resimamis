using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class ParchePropiedadesTests
{
    [Fact]
    public void EsAusente_Null_EsTrue() => Assert.True(ParchePropiedades.EsAusente(null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EsAusente_StringVacioOEspacios_EsTrue(string valor) =>
        Assert.True(ParchePropiedades.EsAusente(valor));

    [Fact]
    public void EsAusente_StringConContenido_EsFalse() =>
        Assert.False(ParchePropiedades.EsAusente("algo"));

    [Fact]
    public void EsAusente_DateTimeDefault_EsTrue() =>
        Assert.True(ParchePropiedades.EsAusente(default(DateTime)));

    [Fact]
    public void EsAusente_DateTimeConValor_EsFalse() =>
        Assert.False(ParchePropiedades.EsAusente(new DateTime(2026, 1, 1)));

    [Theory]
    [InlineData(0)]
    [InlineData(0L)]
    [InlineData((short)0)]
    [InlineData(0.0)]
    public void EsAusente_NumeroCero_EsTrue(object cero) =>
        Assert.True(ParchePropiedades.EsAusente(cero));

    [Theory]
    [InlineData(5)]
    [InlineData(-1)]
    [InlineData(1.5)]
    public void EsAusente_NumeroDistintoDeCero_EsFalse(object valor) =>
        Assert.False(ParchePropiedades.EsAusente(valor));

    [Fact]
    public void EsAusente_DecimalCero_EsTrue() =>
        Assert.True(ParchePropiedades.EsAusente(0m));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EsAusente_Bool_NuncaEsAusente(bool valor)
    {
        // Documenta el contrato: los bool no se tratan como "ausentes" (false NO se descarta).
        Assert.False(ParchePropiedades.EsAusente(valor));
    }

    private class Persona
    {
        public string? Nombre { get; set; }
        public int Edad { get; set; }
        public string? SoloLectura { get; }
    }

    [Fact]
    public void CopiarPresentes_CopiaSoloLosCamposPresentes()
    {
        var origen = new Persona { Nombre = "Nuevo", Edad = 0 }; // Edad 0 = ausente
        var destino = new Persona { Nombre = "Viejo", Edad = 42 };

        ParchePropiedades.CopiarPresentes(origen, destino);

        Assert.Equal("Nuevo", destino.Nombre); // presente → se copia
        Assert.Equal(42, destino.Edad);        // ausente (0) → se conserva
    }

    [Fact]
    public void CopiarPresentes_StringVacioNoPisaElDestino()
    {
        var origen = new Persona { Nombre = "   ", Edad = 7 };
        var destino = new Persona { Nombre = "Original", Edad = 1 };

        ParchePropiedades.CopiarPresentes(origen, destino);

        Assert.Equal("Original", destino.Nombre); // "   " es ausente
        Assert.Equal(7, destino.Edad);
    }

    [Fact]
    public void CopiarPresentes_RespetaElPredicadoIgnorar()
    {
        var origen = new Persona { Nombre = "Nuevo", Edad = 30 };
        var destino = new Persona { Nombre = "Viejo", Edad = 5 };

        ParchePropiedades.CopiarPresentes(origen, destino, ignorar: nombre => nombre == nameof(Persona.Nombre));

        Assert.Equal("Viejo", destino.Nombre); // ignorado
        Assert.Equal(30, destino.Edad);
    }
}
