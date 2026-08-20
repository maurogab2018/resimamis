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
    AbrazosVoluntariaDashboardRespuesta ObtenerAbrazosVoluntaria(int idVoluntaria, DateTime? inicioUtc, DateTime? finUtcExclusivo, DateOnly? fechaConsulta);
    DashboardCoordinacionHoyRespuesta ObtenerCoordinacionHoy(DateTime inicioUtc, DateTime finUtcExclusivo, DateOnly fecha);
    DashboardCoberturaHoyRespuesta ObtenerCoberturaHoy(DateTime inicioUtc, DateTime finUtcExclusivo, DateOnly fecha);
    EstadisticaBebesPorEstadoRespuesta ObtenerBebesPorEstado();
    EstadisticaBebesPorSalaRespuesta ObtenerBebesPorSala();
    RankingVoluntariasAbrazosRespuesta ObtenerRankingVoluntariasAbrazos(DateTime fechaInicio, DateTime fechaFin, int top);
    EvolucionPesoBebesRespuesta ObtenerEvolucionPesoBebes(DateTime? fechaInicio, DateTime? fechaFin);
}
