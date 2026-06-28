using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciaController(INegAsistencia negAsistencia) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var resultado = negAsistencia.ListarTodasAsistencias();
                return ApiResults.Success(resultado);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("id/{IdVoluntaria}")]
        public IActionResult Get(int IdVoluntaria)
        {
            try
            {
                var resultado=negAsistencia.consultarAsistencia(IdVoluntaria);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Reporte de asistencias por período (días inclusive, zona Argentina). Query: fechaInicio y fechaFin (ej. 2026-05-25 o 25/05/2026).</summary>
        [HttpGet("reporte")]
        public IActionResult GetReporteAsistenciaPeriodo(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin,
            [FromQuery(Name = "fecha_inicio")] string? fechaInicioSnake,
            [FromQuery(Name = "fecha_fin")] string? fechaFinSnake,
            [FromQuery(Name = "fechaDesde")] string? fechaDesde,
            [FromQuery(Name = "fechaHasta")] string? fechaHasta)
        {
            try
            {
                var inicioRaw = fechaInicio ?? fechaInicioSnake ?? fechaDesde;
                var finRaw = fechaFin ?? fechaFinSnake ?? fechaHasta;
                if (string.IsNullOrWhiteSpace(inicioRaw) || string.IsNullOrWhiteSpace(finRaw))
                    throw new ApplicationException("Debe indicar fechaInicio y fechaFin en el query (ej. 2026-05-25).");

                var dInicio = NegConversorFecha.ParseFechaCalendarioReporte(inicioRaw);
                var dFin = NegConversorFecha.ParseFechaCalendarioReporte(finRaw);
                ReporteAsistenciaPeriodoRespuesta reporte = negAsistencia.ReporteAsistenciaPorPeriodo(dInicio, dFin);
                return ApiResults.Success(reporte);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("hoy")]
        public IActionResult GetAsistenciasVoluntarias()
        {
            try
            {
                var resultado = negAsistencia.consultarAsistenciasFechahoy();
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("historicas/{IdVoluntaria}")]
        public IActionResult GetAsistenciasVoluntariasHistoricas(int IdVoluntaria)
        {
            try
            {
                var resultado = negAsistencia.consultarAsistenciasVoluntaria(IdVoluntaria);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("entrada/{IdVoluntaria}")]
        public IActionResult Post(int IdVoluntaria)
        {
            try
            {
                var respuesta= negAsistencia.registrarAsistencia(IdVoluntaria);
                return ApiResults.Success(respuesta);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("salida/{IdVoluntaria}")]
        public IActionResult PostSalida(int IdVoluntaria)
        {
            try
            {
                var resultado = negAsistencia.registrarAsistenciaSalida(IdVoluntaria);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Baja lógica: estado Eliminado (ámbito Asistencias).</summary>
        [HttpPost("delete")]
        public IActionResult Delete(int idAsistencia)
        {
            try
            {
                var ok = negAsistencia.eliminarAsistencia(idAsistencia);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
