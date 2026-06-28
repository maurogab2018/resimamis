using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IAsignacionRepositorio
{
    void registrarAsignacion(ASIGNACION asignacion);
    ASIGNACION consultarAsignacion(int idAsignacion);
    bool modificarAsignacion(ASIGNACION datos, ASIGNACION existente);
    bool eliminarAsignacionLogica(int idAsignacion);
    List<ASIGNACION> listarAsignacionesHoy();
    List<ASIGNACION> listarAsignacionesHoyVoluntaria(int idVoluntaria);
    List<ASIGNACION> listarAbrazosHistoricosPorBebe(int idBebe);
    void registrarCambioaAsignacion();
    bool registrarDetalleAsignacion(List<RequestDetalleAsignacion> request);
    EstadisticaDuracionesAbrazos devolverDuracionesAbrazos();
    List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones1();
}
