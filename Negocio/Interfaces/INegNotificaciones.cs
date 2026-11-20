using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegNotificaciones
{
    /// <summary>Clave pública VAPID para que el navegador se suscriba. Null si push no está configurado.</summary>
    string? ClavePublicaPush();

    /// <summary>Guarda (o renueva) la suscripción push del navegador de la voluntaria logueada.</summary>
    void SuscribirPush(int dniSolicitante, RequestSuscripcionPush suscripcion);

    void DesuscribirPush(int dniSolicitante, string endpoint);

    /// <summary>Cantidad de dispositivos con push activo de la voluntaria logueada.</summary>
    int DispositivosConPush(int dniSolicitante);

    /// <summary>Manda una notificación de prueba a los dispositivos de la voluntaria logueada.</summary>
    int ProbarPush(int dniSolicitante);

    /// <summary>
    /// Avisa por push a cada voluntaria del bebé que le asignaron (solo a las que activaron las
    /// notificaciones; no hay mail de respaldo). Va en segundo plano y nunca hace fallar la asignación.
    /// </summary>
    void NotificarAsignaciones(IEnumerable<RespuestaAsignaciones> asignaciones);

    /// <summary>
    /// Push con un mensaje libre a varias voluntarias (lo usa el asistente, siempre con confirmación).
    /// Devuelve, por voluntaria, a cuántos dispositivos llegó.
    /// </summary>
    List<ResultadoPushVoluntaria> EnviarPushAVoluntarias(IEnumerable<int> idsVoluntaria, string titulo, string mensaje);

    /// <summary>Nombre y cantidad de dispositivos con push de cada voluntaria (para la vista previa).</summary>
    List<ResultadoPushVoluntaria> DispositivosPorVoluntaria(IEnumerable<int> idsVoluntaria);

    /// <summary>
    /// Si el insumo quedó en o bajo su stock mínimo, avisa por push a las coordinadoras que activaron
    /// las notificaciones. Va en segundo plano y nunca hace fallar el movimiento.
    /// </summary>
    void AvisarStockBajo(int idInsumo);

    /// <summary>
    /// Push de recordatorio a las voluntarias cuyo turno de hoy empieza dentro de <paramref name="anticipacion"/>
    /// y que todavía no ficharon la entrada. Cada turno se recuerda una sola vez por día.
    /// </summary>
    int EnviarRecordatoriosTurno(TimeSpan anticipacion);

    /// <summary>La coordinadora le manda un mail a una voluntaria. Devuelve el resultado del envío.</summary>
    string EnviarMailAVoluntaria(int dniSolicitante, int idVoluntaria, string asunto, string mensaje);
}
