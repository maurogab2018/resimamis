using ResimamisBackend.Negocio;

namespace ResimamisBackend.Tests.Negocio;

public class NegConversorFechaTests
{
    [Theory]
    [InlineData("2026-11-19")]
    [InlineData("19/11/2026")]
    [InlineData("19-11-2026")]
    [InlineData("9/1/2026")]
    public void TryParseFechaCalendarioReporte_FormatosAceptados_DevuelveEseDia(string valor)
    {
        var ok = NegConversorFecha.TryParseFechaCalendarioReporte(valor, out var fecha);
        Assert.True(ok);
        Assert.Equal(fecha.Date, fecha); // sin componente horario
    }

    [Fact]
    public void TryParseFechaCalendarioReporte_Iso_DevuelveFechaCorrecta()
    {
        Assert.True(NegConversorFecha.TryParseFechaCalendarioReporte("2026-11-19", out var fecha));
        Assert.Equal(new DateTime(2026, 11, 19), fecha);
    }

    [Fact]
    public void TryParseFechaCalendarioReporte_DdMmYyyy_DevuelveFechaCorrecta()
    {
        Assert.True(NegConversorFecha.TryParseFechaCalendarioReporte("19/11/2026", out var fecha));
        Assert.Equal(new DateTime(2026, 11, 19), fecha);
    }

    [Fact]
    public void TryParseFechaCalendarioReporte_IsoConHora_TomaSoloElDia()
    {
        Assert.True(NegConversorFecha.TryParseFechaCalendarioReporte("2026-11-19T22:30:00Z", out var fecha));
        Assert.Equal(new DateTime(2026, 11, 19), fecha);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("no es fecha")]
    [InlineData("32/13/2026")]
    public void TryParseFechaCalendarioReporte_Invalido_EsFalse(string? valor)
    {
        Assert.False(NegConversorFecha.TryParseFechaCalendarioReporte(valor, out _));
    }

    [Fact]
    public void ParseFechaCalendarioReporte_Invalido_LanzaApplicationException()
    {
        Assert.Throws<ApplicationException>(() => NegConversorFecha.ParseFechaCalendarioReporte("fecha mala"));
    }

    [Fact]
    public void RangoFechasArgentinaEnUtc_FinAnteriorAInicio_Lanza()
    {
        Assert.Throws<ApplicationException>(() =>
            NegConversorFecha.RangoFechasArgentinaEnUtc(new DateTime(2026, 11, 19), new DateTime(2026, 11, 18)));
    }

    [Fact]
    public void RangoFechasArgentinaEnUtc_SuperaMaximo_Lanza()
    {
        Assert.Throws<ApplicationException>(() =>
            NegConversorFecha.RangoFechasArgentinaEnUtc(new DateTime(2026, 1, 1), new DateTime(2026, 1, 11), maxDiasPermitidos: 5));
    }

    [Fact]
    public void RangoFechasArgentinaEnUtc_UnDia_DevuelveVentanaDe24hEnUtc()
    {
        var (inicio, fin) = NegConversorFecha.RangoFechasArgentinaEnUtc(
            new DateTime(2026, 11, 19), new DateTime(2026, 11, 19));

        Assert.Equal(DateTimeKind.Utc, inicio.Kind);
        Assert.Equal(DateTimeKind.Utc, fin.Kind);
        Assert.Equal(TimeSpan.FromDays(1), fin - inicio);
        // Argentina es UTC-3 todo el año (sin horario de verano): medianoche AR = 03:00 UTC.
        Assert.Equal(3, inicio.Hour);
    }

    [Fact]
    public void RangoFechasArgentinaEnUtc_RangoInclusivo_CuentaAmbosExtremos()
    {
        var (inicio, fin) = NegConversorFecha.RangoFechasArgentinaEnUtc(
            new DateTime(2026, 11, 1), new DateTime(2026, 11, 10));
        // 10 días calendario inclusivos.
        Assert.Equal(TimeSpan.FromDays(10), fin - inicio);
    }

    [Fact]
    public void FechaCalendarioArgentina_InstanteUtcDeMadrugada_DevuelveDiaAnteriorEnAr()
    {
        // 19/11 02:00 UTC == 18/11 23:00 en Argentina (UTC-3).
        var utc = new DateTime(2026, 11, 19, 2, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 11, 18), NegConversorFecha.FechaCalendarioArgentina(utc));
    }

    [Fact]
    public void FechaCalendarioArgentina_InstanteUtcDeManana_DevuelveMismoDiaEnAr()
    {
        // 19/11 05:00 UTC == 19/11 02:00 en Argentina.
        var utc = new DateTime(2026, 11, 19, 5, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 11, 19), NegConversorFecha.FechaCalendarioArgentina(utc));
    }

    [Fact]
    public void DiasDesdeFechaCalendarioHastaHoyArgentina_FechaFutura_Devuelve0()
    {
        var futuro = DateTime.UtcNow.AddDays(30);
        Assert.Equal(0, NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(futuro));
    }

    [Fact]
    public void DiasDesdeFechaCalendarioHastaHoyArgentina_FechaPasada_DevuelvePositivo()
    {
        var pasado = DateTime.UtcNow.AddDays(-10);
        Assert.True(NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(pasado) >= 9);
    }
}
