using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegAsistencia
{
    bool registrarAsistencia(int idVoluntaria);
    ASISTENCIA? consultarAsistencia(int idVoluntaria);
    List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria);
    List<ASISTENCIA> consultarAsistenciasFechahoy();
    bool registrarAsistenciaSalida(int idVoluntaria);
    bool eliminarAsistencia(int idAsistencia);
    List<ReporteAsistenciaPeriodoItem> ListarTodasAsistencias();
    ReporteAsistenciaPeriodoRespuesta ReporteAsistenciaPorPeriodo(DateTime fechaInicio, DateTime fechaFin);
}
