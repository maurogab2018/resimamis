using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Finaliza automáticamente, al terminar cada día (hora Argentina), los abrazos que quedaron
    /// iniciados y las asignaciones del día que nunca se iniciaron. Los finalizados a mano no se
    /// tocan. Corre también al arrancar: en Render el servicio puede estar dormido a medianoche y
    /// así el primer request del día siguiente encuentra el día anterior ya cerrado.
    /// </summary>
    public class CierreAbrazosDiarioServicio(
        IServiceScopeFactory scopeFactory,
        ILogger<CierreAbrazosDiarioServicio> logger) : BackgroundService
    {
        // Margen pasada la medianoche para no caer en el borde del rango "hoy".
        private static readonly TimeSpan MargenPostMedianoche = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CerrarDiasAnteriores();

                var (_, finHoyUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
                var espera = finHoyUtc + MargenPostMedianoche - DateTime.UtcNow;
                if (espera < TimeSpan.Zero)
                    espera = MargenPostMedianoche;

                try
                {
                    await Task.Delay(espera, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private void CerrarDiasAnteriores()
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var negAsignacion = scope.ServiceProvider.GetRequiredService<INegAsignacion>();
                var cantidad = negAsignacion.CerrarAbrazosSinFinalizar(incluirEnCursoDeHoy: false);
                if (cantidad > 0)
                    logger.LogInformation("Cierre diario: {Cantidad} abrazo(s) finalizados automáticamente.", cantidad);
            }
            catch (Exception ex)
            {
                // Un fallo (p. ej. la base no responde) no debe tirar el host; se reintenta en el próximo ciclo.
                logger.LogError(ex, "Cierre diario de abrazos falló.");
            }
        }
    }
}
