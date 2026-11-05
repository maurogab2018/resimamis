using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegEstadisticasAvanzadas : INegEstadisticasAvanzadas
    {
        private const int DiasRangoDefault = 90;

        private readonly IEstadisticasAvanzadasRepositorio repositorio;

        public NegEstadisticasAvanzadas(IEstadisticasAvanzadasRepositorio repositorio)
        {
            this.repositorio = repositorio;
        }

        /// <summary>Sin fechas: últimos 90 días hasta hoy. Valida el orden y el máximo de 2 años.</summary>
        private static (DateTime Desde, DateTime Hasta) Rango(DateTime? desde, DateTime? hasta)
        {
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
            var h = (hasta ?? hoy).Date;
            var d = (desde ?? h.AddDays(-(DiasRangoDefault - 1))).Date;
            // Misma validación que el resto de los reportes (orden y amplitud máxima).
            NegConversorFecha.RangoFechasArgentinaEnUtc(d, h);
            return (d, h);
        }

        private static int Entre(int? valor, int porDefecto, int min, int max, string nombre)
        {
            var v = valor ?? porDefecto;
            if (v < min || v > max)
                throw new ApplicationException($"{nombre} tiene que estar entre {min} y {max}.");
            return v;
        }

        public ImpactoProgramaRespuesta ObtenerImpactoPrograma(DateTime? egresoDesde, DateTime? egresoHasta)
        {
            if (egresoDesde.HasValue && egresoHasta.HasValue && egresoHasta < egresoDesde)
                throw new ApplicationException("La fecha fin no puede ser anterior a la fecha inicio.");
            return repositorio.ObtenerImpactoPrograma(egresoDesde, egresoHasta);
        }

        public PrimerAbrazoRespuesta ObtenerPrimerAbrazo() => repositorio.ObtenerPrimerAbrazo();

        public EquidadBebesRespuesta ObtenerEquidadBebes() => repositorio.ObtenerEquidadBebes();

        public CumplimientoHorarioRespuesta ObtenerCumplimientoHorario(DateTime? desde, DateTime? hasta)
        {
            // Por defecto, los últimos 30 días: el horario cargado es el actual y un rango largo lo distorsiona.
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
            var (d, h) = Rango(desde ?? (hasta ?? hoy).AddDays(-29), hasta);
            return repositorio.ObtenerCumplimientoHorario(d, h);
        }

        public ProyeccionInsumosRespuesta ObtenerProyeccionInsumos(int? diasAnalizados) =>
            repositorio.ObtenerProyeccionInsumos(Entre(diasAnalizados, 30, 7, 365, "dias"));

        public MapaCalorRespuesta ObtenerMapaCalor(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            return repositorio.ObtenerMapaCalor(d, h);
        }

        public TasaCompletadosRespuesta ObtenerTasaCompletados(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            return repositorio.ObtenerTasaCompletados(d, h);
        }

        public BebesSinVisitasRespuesta ObtenerBebesSinVisitas(int? umbralDias) =>
            repositorio.ObtenerBebesSinVisitas(Entre(umbralDias, 7, 1, 365, "dias"));

        public RendimientoVoluntariasRespuesta ObtenerRendimientoVoluntarias(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            return repositorio.ObtenerRendimientoVoluntarias(d, h);
        }

        public DuracionPorGrupoRespuesta ObtenerDuracionPorGrupo(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            return repositorio.ObtenerDuracionPorGrupo(d, h);
        }
    }
}
