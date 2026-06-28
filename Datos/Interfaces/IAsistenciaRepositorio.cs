using ResimamisBackend.Datos;

namespace ResimamisBackend.Datos.Interfaces;

public interface IAsistenciaRepositorio
{
    bool registrarAsistencia(ASISTENCIA asistencia);
    ASISTENCIA? consultarAsistencia(int idVoluntaria);
    bool registrarAsistenciaSalida(int idVoluntaria);
    List<ASISTENCIA> consultarAsistenciasFechahoy();
    List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria);
    bool eliminarAsistenciaLogico(int idAsistencia);
    List<ASISTENCIA> ListarTodasAsistencias();
    List<ASISTENCIA> ListarAsistenciasPorPeriodoUtc(DateTime inicioUtc, DateTime finUtcExclusivo);
}
