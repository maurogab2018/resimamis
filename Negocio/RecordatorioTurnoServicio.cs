using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Cada 5 minutos manda un push a las voluntarias cuyo turno de hoy empieza en la próxima hora
    /// (configurable con Push:RecordatorioTurnoMinutos; 0 lo apaga). Solo llega a quienes activaron
    /// las notificaciones y todavía no ficharon la entrada.
    /// </summary>
    public class RecordatorioTurnoServicio(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RecordatorioTurnoServicio> logger) : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var minutos = configuration.GetValue("Push:RecordatorioTurnoMinutos", 60);
            if (minutos <= 0)
            {
                logger.LogInformation("Recordatorio de turno desactivado (Push:RecordatorioTurnoMinutos = 0).");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var notificaciones = scope.ServiceProvider.GetRequiredService<INegNotificaciones>();
                    notificaciones.EnviarRecordatoriosTurno(TimeSpan.FromMinutes(minutos));
                }
                catch (Exception ex)
                {
                    // Un fallo (la base no responde, push mal configurado) no debe tirar el host.
                    logger.LogError(ex, "Recordatorio de turno falló.");
                }

                try
                {
                    await Task.Delay(Intervalo, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}
