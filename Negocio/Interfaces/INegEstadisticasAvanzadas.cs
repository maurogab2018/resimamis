using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

/// <summary>Rangos opcionales: sin fechas, los últimos 90 días (hora Argentina).</summary>
public interface INegEstadisticasAvanzadas
{
    ImpactoProgramaRespuesta ObtenerImpactoPrograma(DateTime? egresoDesde, DateTime? egresoHasta);
    PrimerAbrazoRespuesta ObtenerPrimerAbrazo();
    EquidadBebesRespuesta ObtenerEquidadBebes();
    CumplimientoHorarioRespuesta ObtenerCumplimientoHorario(DateTime? desde, DateTime? hasta);
    ProyeccionInsumosRespuesta ObtenerProyeccionInsumos(int? diasAnalizados);
    MapaCalorRespuesta ObtenerMapaCalor(DateTime? desde, DateTime? hasta);
    TasaCompletadosRespuesta ObtenerTasaCompletados(DateTime? desde, DateTime? hasta);
    BebesSinVisitasRespuesta ObtenerBebesSinVisitas(int? umbralDias);
    RendimientoVoluntariasRespuesta ObtenerRendimientoVoluntarias(DateTime? desde, DateTime? hasta);
    DuracionPorGrupoRespuesta ObtenerDuracionPorGrupo(DateTime? desde, DateTime? hasta);
}
