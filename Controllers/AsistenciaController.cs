using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciaController: ControllerBase
    {

        public readonly NegAsistencia negAsistencia;
        public AsistenciaController()
        {
            negAsistencia = new NegAsistencia();
        }

        [HttpGet("id/{IdVoluntaria}")]
        public IActionResult Get(int IdVoluntaria)
        {
            try
            {
                var resultado=negAsistencia.consultarAsistencia(IdVoluntaria);
                return ApiResults.Success(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);  
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }    
        }

        /// <summary>Reporte de asistencias por período (días inclusive, zona Argentina). Query: fechaInicio, fechaFin (ej. 2026-01-01).</summary>
        [HttpGet("reporte")]
        public IActionResult GetReporteAsistenciaPeriodo([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
        {
            try
            {
                ReporteAsistenciaPeriodoRespuesta reporte = negAsistencia.ReporteAsistenciaPorPeriodo(fechaInicio, fechaFin);
                return ApiResults.Success(reporte);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
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
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
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
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost("entrada/{IdVoluntaria}")]
        public IActionResult Post(int IdVoluntaria)
        {
            try
            {
                var respuesta= negAsistencia.registrarAsistencia(IdVoluntaria);
                return ApiResults.Success(new {respuesta= respuesta});
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
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
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Baja lógica: estado Eliminado (ámbito Asistencias).</summary>
        [HttpPost("delete")]
        public IActionResult Delete(int idAsistencia)
        {
            try
            {
                var ok = negAsistencia.eliminarAsistencia(idAsistencia);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }
    }
}
