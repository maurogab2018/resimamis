using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegAsistente
{
    /// <summary>Estado del asistente; con dni incluye cuántas preguntas lleva hoy esa usuaria.</summary>
    AsistenteEstadoRespuesta ObtenerEstado(int? dniSolicitante = null);
    Task<AsistentePreguntaRespuesta> Preguntar(
        int dniSolicitante,
        AsistentePreguntaRequest request,
        CancellationToken cancellationToken = default);
}
