using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    /// <summary>
    /// Estadísticas de análisis (solo lectura). Las de Dashboard siguen igual; estas responden
    /// preguntas de impacto, equidad, cumplimiento y previsión. Fechas: yyyy-MM-dd o dd/MM/yyyy;
    /// sin fechas, los últimos 90 días (cumplimiento: 30).
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EstadisticasController(INegEstadisticasAvanzadas neg) : ControllerBase
    {
        private static DateTime? Fecha(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : NegConversorFecha.ParseFechaCalendarioReporte(valor);

        private IActionResult Ejecutar(Func<object> accion)
        {
            try
            {
                return ApiResults.Success(accion());
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Bebés egresados agrupados por abrazos recibidos: ganancia de peso por día y días de internación. Query opcional: fechaDesde/fechaHasta del egreso.</summary>
        [HttpGet("impacto")]
        public IActionResult GetImpacto([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerImpactoPrograma(Fecha(fechaDesde), Fecha(fechaHasta)));

        /// <summary>Días desde el ingreso a NEO hasta el primer abrazo: promedio, mediana, por mes de ingreso y por bebé.</summary>
        [HttpGet("primer-abrazo")]
        public IActionResult GetPrimerAbrazo() => Ejecutar(() => neg.ObtenerPrimerAbrazo());

        /// <summary>Abrazos por semana de internación de cada bebé activo, marcando a los que reciben menos de la mitad del promedio.</summary>
        [HttpGet("equidad-bebes")]
        public IActionResult GetEquidad() => Ejecutar(() => neg.ObtenerEquidadBebes());

        /// <summary>Días del horario cargado contra días en que la voluntaria vino. Sin fechas: últimos 30 días.</summary>
        [HttpGet("cumplimiento-horario")]
        public IActionResult GetCumplimiento([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerCumplimientoHorario(Fecha(fechaDesde), Fecha(fechaHasta)));

        /// <summary>Días hasta el mínimo y hasta agotarse de cada insumo según el consumo de los últimos N días (dias, default 30).</summary>
        [HttpGet("proyeccion-insumos")]
        public IActionResult GetProyeccion([FromQuery] int? dias) => Ejecutar(() => neg.ObtenerProyeccionInsumos(dias));

        /// <summary>Abrazos y asistencias por día de la semana y hora (Argentina).</summary>
        [HttpGet("mapa-calor")]
        public IActionResult GetMapaCalor([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerMapaCalor(Fecha(fechaDesde), Fecha(fechaHasta)));

        /// <summary>Asignaciones de abrazo completadas, cerradas automáticamente o nunca iniciadas; total, por voluntaria y por día de la semana.</summary>
        [HttpGet("tasa-completados")]
        public IActionResult GetTasaCompletados([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerTasaCompletados(Fecha(fechaDesde), Fecha(fechaHasta)));

        /// <summary>Bebés activos sin visita familiar hace al menos N días (dias, default 7).</summary>
        [HttpGet("bebes-sin-visitas")]
        public IActionResult GetBebesSinVisitas([FromQuery] int? dias) => Ejecutar(() => neg.ObtenerBebesSinVisitas(dias));

        /// <summary>Por voluntaria: horas en el hospital, abrazos, abrazos por hora y porcentaje del tiempo abrazando.</summary>
        [HttpGet("rendimiento-voluntarias")]
        public IActionResult GetRendimiento([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerRendimientoVoluntarias(Fecha(fechaDesde), Fecha(fechaHasta)));

        /// <summary>Duración promedio y mediana de los abrazos por sala y por tramo de peso del bebé.</summary>
        [HttpGet("duracion-por-grupo")]
        public IActionResult GetDuracionPorGrupo([FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta) =>
            Ejecutar(() => neg.ObtenerDuracionPorGrupo(Fecha(fechaDesde), Fecha(fechaHasta)));
    }
}
