using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

/// <summary>Estadísticas de análisis (api/Estadisticas). Rangos: fechas calendario Argentina, inclusive.</summary>
public interface IEstadisticasAvanzadasRepositorio
{
    ImpactoProgramaRespuesta ObtenerImpactoPrograma(DateTime? egresoDesde, DateTime? egresoHasta);
    PrimerAbrazoRespuesta ObtenerPrimerAbrazo();
    EquidadBebesRespuesta ObtenerEquidadBebes();
    CumplimientoHorarioRespuesta ObtenerCumplimientoHorario(DateTime desde, DateTime hasta);
    ProyeccionInsumosRespuesta ObtenerProyeccionInsumos(int diasAnalizados);
    MapaCalorRespuesta ObtenerMapaCalor(DateTime desde, DateTime hasta);
    TasaCompletadosRespuesta ObtenerTasaCompletados(DateTime desde, DateTime hasta);
    BebesSinVisitasRespuesta ObtenerBebesSinVisitas(int umbralDias);
    RendimientoVoluntariasRespuesta ObtenerRendimientoVoluntarias(DateTime desde, DateTime hasta);
    DuracionPorGrupoRespuesta ObtenerDuracionPorGrupo(DateTime desde, DateTime hasta);
}
