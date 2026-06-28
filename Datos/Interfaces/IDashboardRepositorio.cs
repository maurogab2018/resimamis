using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IDashboardRepositorio
{
    EstadisticaAsignacionesPorDiaRespuesta ObtenerAsignacionesPorDia(DateTime fechaInicio, DateTime fechaFin);
    EstadisticaDuracionAbrazosRespuesta ObtenerDuracionAbrazos(DateTime? fechaInicio, DateTime? fechaFin);
    EstadisticaRangoEdadesBebesRespuesta ObtenerRangoEdadesBebes();
    EstadisticaPermanenciaBebesRespuesta ObtenerPermanenciaBebes();
    EstadisticaVisitasRespuesta ObtenerEstadisticasVisitas(DateTime fechaInicio, DateTime fechaFin);
    DashboardResumenRespuesta ObtenerResumen(DateTime fechaInicio, DateTime fechaFin);
    AbrazosBebeDashboardRespuesta ObtenerAbrazosBebe(int idBebe, DateTime? inicioUtc, DateTime? finUtcExclusivo, DateOnly? fechaConsulta);
}
