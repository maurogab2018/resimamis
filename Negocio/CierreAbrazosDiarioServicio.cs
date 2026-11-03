using ResimamisBackend.Datos;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Finaliza automáticamente, una vez por día (hora Argentina), los abrazos que quedaron
    /// iniciados y las asignaciones de días anteriores que nunca se iniciaron. Los finalizados a
    /// mano no se tocan. Se intenta al arrancar y a las 00:05: en Render el servicio puede estar
    /// dormido a medianoche y despertarse varias veces por día, así que antes de correr se fija si
    /// ya hay una marca "Ejecución cierre diario" de hoy en REGISTROACTIVIDAD y, si la hay, no hace
    /// nada. La marca también deja rastro aunque no se haya cerrado ningún abrazo.
    /// </summary>
    public class CierreAbrazosDiarioServicio(
        IServiceScopeFactory scopeFactory,
        ILogger<CierreAbrazosDiarioServicio> logger) : BackgroundService
    {
        // Margen pasada la medianoche para no caer en el borde del rango "hoy".
        private static readonly TimeSpan MargenPostMedianoche = TimeSpan.FromMinutes(5);

        /// <summary>Próxima corrida programada (en memoria; se recalcula en cada arranque).</summary>
        public static DateTime? ProximaEjecucionUtc { get; private set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var motivo = "arranque del backend";
            while (!stoppingToken.IsCancellationRequested)
            {
                CerrarDiasAnterioresSiNoCorrioHoy(motivo);

                var (_, finHoyUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
                var espera = finHoyUtc + MargenPostMedianoche - DateTime.UtcNow;
                if (espera < TimeSpan.Zero)
                    espera = MargenPostMedianoche;
                ProximaEjecucionUtc = DateTime.UtcNow + espera;

                try
                {
                    await Task.Delay(espera, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                motivo = "horario diario (00:05)";
            }
        }

        private void CerrarDiasAnterioresSiNoCorrioHoy(string motivo)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var (inicioHoyUtc, _) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

                var yaCorrioHoy = db.REGISTROACTIVIDAD.Any(r =>
                    r.tipoMovimiento == TiposMovimientoActividad.EjecucionCierreDiario
                    && r.fechaHora >= inicioHoyUtc);
                if (yaCorrioHoy)
                {
                    logger.LogInformation("Cierre diario ({Motivo}): ya corrió hoy, no se repite.", motivo);
                    return;
                }

                var negAsignacion = scope.ServiceProvider.GetRequiredService<INegAsignacion>();
                var cantidad = negAsignacion.CerrarAbrazosSinFinalizar(incluirEnCursoDeHoy: false);

                db.REGISTROACTIVIDAD.Add(new REGISTROACTIVIDAD
                {
                    fechaHora = NegConversorFecha.ObtenerFechaArgentina(),
                    tipoMovimiento = TiposMovimientoActividad.EjecucionCierreDiario,
                    tipoRegistro = TiposRegistroActividad.Sistema,
                    idRegistro = cantidad,
                    descripcion = $"Cierre diario ({motivo}): {cantidad} abrazo(s) finalizados."
                });
                db.SaveChanges();
                logger.LogInformation("Cierre diario ({Motivo}): {Cantidad} abrazo(s) finalizados automáticamente.", motivo, cantidad);
            }
            catch (Exception ex)
            {
                // Un fallo (p. ej. la base no responde) no debe tirar el host. Sin marca, el próximo
                // arranque o la corrida de las 00:05 lo vuelve a intentar.
                logger.LogError(ex, "Cierre diario de abrazos falló.");
            }
        }
    }
}
