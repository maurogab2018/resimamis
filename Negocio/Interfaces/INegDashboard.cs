using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegDashboard
{
    EstadisticaAsignacionesPorDiaRespuesta ObtenerAsignacionesPorDia(DateTime fechaInicio, DateTime fechaFin);
    EstadisticaDuracionAbrazosRespuesta ObtenerDuracionAbrazos(DateTime? fechaInicio, DateTime? fechaFin);
    EstadisticaRangoEdadesBebesRespuesta ObtenerRangoEdadesBebes();
    EstadisticaPermanenciaBebesRespuesta ObtenerPermanenciaBebes();
    EstadisticaVisitasRespuesta ObtenerEstadisticasVisitas(DateTime fechaInicio, DateTime fechaFin);
    DashboardResumenRespuesta ObtenerResumen(DateTime fechaInicio, DateTime fechaFin);
    AbrazosBebeDashboardRespuesta ObtenerAbrazosBebeHoy(int idBebe);
    AbrazosBebeDashboardRespuesta ObtenerAbrazosBebeHistorial(int idBebe, DateTime? fechaInicio, DateTime? fechaFin);
}
