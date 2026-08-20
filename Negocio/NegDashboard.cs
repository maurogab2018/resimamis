using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegDashboard : INegDashboard
    {
        private readonly IDashboardRepositorio dashboardRepositorio;

        public NegDashboard(IDashboardRepositorio dashboardRepositorio)
        {
            this.dashboardRepositorio = dashboardRepositorio;
        }

        public EstadisticaAsignacionesPorDiaRespuesta ObtenerAsignacionesPorDia(DateTime fechaInicio, DateTime fechaFin) =>
            dashboardRepositorio.ObtenerAsignacionesPorDia(fechaInicio, fechaFin);

        public EstadisticaDuracionAbrazosRespuesta ObtenerDuracionAbrazos(DateTime? fechaInicio, DateTime? fechaFin) =>
            dashboardRepositorio.ObtenerDuracionAbrazos(fechaInicio, fechaFin);

        public EstadisticaRangoEdadesBebesRespuesta ObtenerRangoEdadesBebes() =>
            dashboardRepositorio.ObtenerRangoEdadesBebes();

        public EstadisticaPermanenciaBebesRespuesta ObtenerPermanenciaBebes() =>
            dashboardRepositorio.ObtenerPermanenciaBebes();

        public EstadisticaVisitasRespuesta ObtenerEstadisticasVisitas(DateTime fechaInicio, DateTime fechaFin) =>
            dashboardRepositorio.ObtenerEstadisticasVisitas(fechaInicio, fechaFin);

        public DashboardResumenRespuesta ObtenerResumen(DateTime fechaInicio, DateTime fechaFin) =>
            dashboardRepositorio.ObtenerResumen(fechaInicio, fechaFin);

        public AbrazosBebeDashboardRespuesta ObtenerAbrazosBebeHoy(int idBebe)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return dashboardRepositorio.ObtenerAbrazosBebe(idBebe, inicioUtc, finUtc, hoy);
        }

        public AbrazosBebeDashboardRespuesta ObtenerAbrazosBebeHistorial(int idBebe, DateTime? fechaInicio, DateTime? fechaFin)
        {
            DateTime? inicioUtc = null;
            DateTime? finUtc = null;
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var rango = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio.Value, fechaFin.Value);
                inicioUtc = rango.InicioUtc;
                finUtc = rango.FinUtcExclusivo;
            }

            return dashboardRepositorio.ObtenerAbrazosBebe(idBebe, inicioUtc, finUtc, null);
        }

        public AbrazosVoluntariaDashboardRespuesta ObtenerAbrazosVoluntariaHoy(int idVoluntaria)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return dashboardRepositorio.ObtenerAbrazosVoluntaria(idVoluntaria, inicioUtc, finUtc, hoy);
        }

        public AbrazosVoluntariaDashboardRespuesta ObtenerAbrazosVoluntariaHistorial(
            int idVoluntaria,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            DateTime? inicioUtc = null;
            DateTime? finUtc = null;
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var rango = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio.Value, fechaFin.Value);
                inicioUtc = rango.InicioUtc;
                finUtc = rango.FinUtcExclusivo;
            }

            return dashboardRepositorio.ObtenerAbrazosVoluntaria(idVoluntaria, inicioUtc, finUtc, null);
        }

        public DashboardCoordinacionHoyRespuesta ObtenerCoordinacionHoy()
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var fecha = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return dashboardRepositorio.ObtenerCoordinacionHoy(inicioUtc, finUtc, fecha);
        }

        public DashboardCoberturaHoyRespuesta ObtenerCoberturaHoy()
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var fecha = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return dashboardRepositorio.ObtenerCoberturaHoy(inicioUtc, finUtc, fecha);
        }

        public EstadisticaBebesPorEstadoRespuesta ObtenerBebesPorEstado() =>
            dashboardRepositorio.ObtenerBebesPorEstado();

        public EstadisticaBebesPorSalaRespuesta ObtenerBebesPorSala() =>
            dashboardRepositorio.ObtenerBebesPorSala();

        public RankingVoluntariasAbrazosRespuesta ObtenerRankingVoluntariasAbrazos(
            DateTime fechaInicio,
            DateTime fechaFin,
            int top) =>
            dashboardRepositorio.ObtenerRankingVoluntariasAbrazos(fechaInicio, fechaFin, top);

        public EvolucionPesoBebesRespuesta ObtenerEvolucionPesoBebes(DateTime? fechaInicio, DateTime? fechaFin) =>
            dashboardRepositorio.ObtenerEvolucionPesoBebes(fechaInicio, fechaFin);
    }
}
