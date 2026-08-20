using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegAsistente
{
    AsistenteEstadoRespuesta ObtenerEstado();
    Task<AsistentePreguntaRespuesta> Preguntar(int dniSolicitante, AsistentePreguntaRequest request);
}
