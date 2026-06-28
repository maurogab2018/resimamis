using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController(INegDashboard negDashboard) : ControllerBase
    {
        private static (DateTime Inicio, DateTime Fin) ParseRangoFechasObligatorio(
            string? fechaDesde,
            string? fechaHasta,
            string? fechaInicio,
            string? fechaFin)
        {
            var inicioRaw = fechaDesde ?? fechaInicio;
            var finRaw = fechaHasta ?? fechaFin;
            if (string.IsNullOrWhiteSpace(inicioRaw) || string.IsNullOrWhiteSpace(finRaw))
                throw new ApplicationException("Debe indicar fechaDesde y fechaHasta (ej. 2026-06-01).");

            var dInicio = NegConversorFecha.ParseFechaCalendarioReporte(inicioRaw);
            var dFin = NegConversorFecha.ParseFechaCalendarioReporte(finRaw);
            return (dInicio, dFin);
        }

        private static (DateTime? Inicio, DateTime? Fin) ParseRangoFechasOpcional(
            string? fechaDesde,
            string? fechaHasta,
            string? fechaInicio,
            string? fechaFin)
        {
            var inicioRaw = fechaDesde ?? fechaInicio;
            var finRaw = fechaHasta ?? fechaFin;
            if (string.IsNullOrWhiteSpace(inicioRaw) && string.IsNullOrWhiteSpace(finRaw))
                return (null, null);

            if (string.IsNullOrWhiteSpace(inicioRaw) || string.IsNullOrWhiteSpace(finRaw))
                throw new ApplicationException("Debe indicar fechaDesde y fechaHasta juntas, o omitir ambas.");

            var dInicio = NegConversorFecha.ParseFechaCalendarioReporte(inicioRaw);
            var dFin = NegConversorFecha.ParseFechaCalendarioReporte(finRaw);
            return (dInicio, dFin);
        }

        /// <summary>Cantidad de asignaciones y abrazos por día calendario (Argentina).</summary>
        [HttpGet("asignaciones/por-dia")]
        public IActionResult GetAsignacionesPorDia(
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            try
            {
                var (inicio, fin) = ParseRangoFechasObligatorio(fechaDesde, fechaHasta, fechaInicio, fechaFin);
                var resultado = negDashboard.ObtenerAsignacionesPorDia(inicio, fin);
                return ApiResults.Success(resultado);
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

        /// <summary>Estadísticas de duración de abrazos finalizados. Filtro opcional por fechaDesde/fechaHasta.</summary>
        [HttpGet("abrazos/duracion")]
        public IActionResult GetDuracionAbrazos(
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            try
            {
                var (inicio, fin) = ParseRangoFechasOpcional(fechaDesde, fechaHasta, fechaInicio, fechaFin);
                var resultado = negDashboard.ObtenerDuracionAbrazos(inicio, fin);
                return ApiResults.Success(resultado);
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

        /// <summary>Abrazos del día (Argentina) para un bebé.</summary>
        [HttpGet("bebe/{idBebe}/abrazos-hoy")]
        public IActionResult GetAbrazosBebeHoy(int idBebe)
        {
            try
            {
                var resultado = negDashboard.ObtenerAbrazosBebeHoy(idBebe);
                return ApiResults.Success(resultado);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
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

        /// <summary>Historial de abrazos de un bebé. Filtro opcional por fechaDesde/fechaHasta.</summary>
        [HttpGet("bebe/{idBebe}/abrazos-historial")]
        public IActionResult GetAbrazosBebeHistorial(
            int idBebe,
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            try
            {
                var (inicio, fin) = ParseRangoFechasOpcional(fechaDesde, fechaHasta, fechaInicio, fechaFin);
                var resultado = negDashboard.ObtenerAbrazosBebeHistorial(idBebe, inicio, fin);
                return ApiResults.Success(resultado);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
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

        /// <summary>Distribución de bebés activos por rango de edad (días desde nacimiento).</summary>
        [HttpGet("bebes/rango-edades")]
        public IActionResult GetRangoEdadesBebes()
        {
            try
            {
                var resultado = negDashboard.ObtenerRangoEdadesBebes();
                return ApiResults.Success(resultado);
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

        /// <summary>Tiempo de permanencia en NEO (días desde FechaIngresoNEO).</summary>
        [HttpGet("bebes/permanencia")]
        public IActionResult GetPermanenciaBebes()
        {
            try
            {
                var resultado = negDashboard.ObtenerPermanenciaBebes();
                return ApiResults.Success(resultado);
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

        /// <summary>Estadísticas de visitas en un período.</summary>
        [HttpGet("visitas/estadisticas")]
        public IActionResult GetEstadisticasVisitas(
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            try
            {
                var (inicio, fin) = ParseRangoFechasObligatorio(fechaDesde, fechaHasta, fechaInicio, fechaFin);
                var resultado = negDashboard.ObtenerEstadisticasVisitas(inicio, fin);
                return ApiResults.Success(resultado);
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

        /// <summary>Resumen general del dashboard para un período.</summary>
        [HttpGet("resumen")]
        public IActionResult GetResumen(
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin)
        {
            try
            {
                var (inicio, fin) = ParseRangoFechasObligatorio(fechaDesde, fechaHasta, fechaInicio, fechaFin);
                var resultado = negDashboard.ObtenerResumen(inicio, fin);
                return ApiResults.Success(resultado);
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
    }
}
