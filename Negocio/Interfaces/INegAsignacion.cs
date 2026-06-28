using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegAsignacion
{
    List<RespuestaAsignaciones> generarAsiganacionTareasPorId(RequestAsignacionTareas requestAsignacion);
    List<RespuestaAsignaciones> generarAsignacionesSeleccion(RequestAsignacionTareas requestAsignacion);
    RespuestaAsignaciones generarAsiganacionTarea(RequestAsignacionTarea requestAsignacion);
    RespuestaAsignaciones generarAsignacionTareaCatalogo(RequestAsignacionTarea requestAsignacion);
    List<RespuestaAsignaciones> generarAsiganaciones();
    bool registrarInicioAsignacionAbrazo(int idAsignacion);
    bool registrarFinAsignacionAbrazo(int idAsignacion, string comentario);
    int ResetearAbrazosBebeColgadosAntesDeHoy();
    bool eliminarAsignacion(int idAsignacion);
    bool modificarAsignacion(int idAsignacion, RespuestaAsignaciones datos);
    RespuestaAsignaciones consultarAsignacionPorId(int idAsignacion);
    List<RespuestaAsignaciones> listarAbrazosHistoricos(int idBebe);
    List<RespuestaAsignaciones>? listarAsignacionesHoy(int dniSolicitante);
    EstadisticaDuracionesAbrazos devolverDuracionesAbrazos();
    List<RespuestaAsignaciones> listarAsignacionesHoyVoluntaria(int idVoluntaria);
    bool registrarDetalleAsignacion(List<RequestDetalleAsignacion> request);
    List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones();
}
