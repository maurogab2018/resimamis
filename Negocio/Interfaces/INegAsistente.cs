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

    /// <summary>Calificación opcional de una respuesta propia: 1 = bien, -1 = mal, 0 = quitar el voto.</summary>
    void Calificar(int dniSolicitante, AsistenteCalificarRequest request);

    /// <summary>Últimas preguntas registradas (solo coordinadoras). soloMalas: 👎 o que no terminaron "ok".</summary>
    List<AsistenteRegistroItem> ObtenerRegistros(int dniSolicitante, bool soloMalas, int limite);
}
