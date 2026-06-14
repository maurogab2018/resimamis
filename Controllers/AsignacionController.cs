using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsignacionController: ControllerBase
    {
        public readonly NegAsignacion negAsignacion;

        public AsignacionController()
        {
            negAsignacion = new NegAsignacion();
        }

        [HttpGet("listarAsignacionesHoy")]
        public IActionResult Get()
        {
            try
            {
                var respuesta = negAsignacion.listarAsignacionesHoy();
                return ApiResults.Success(new { listadoAsignaciones = respuesta });
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

        [HttpGet("consultar/{idAsignacion}")]
        public IActionResult GetId(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.consultarAsignacionPorId(idAsignacion);
                return ApiResults.Success(new { asignacion = respuesta });
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


        [HttpGet("listarCantidadAsignacionesPorDia")]
        public IActionResult GetCantidadAsignaciones(/*RequestEstadisticaCantidadAsignaciones request*/)
        {
            try
            {
                var respuesta = negAsignacion.devolverEstadisticaCantidadAsignaciones(/*request.fechaInicio,request.fechaFin*/);
                return ApiResults.Success(new { listadoAsignaciones = respuesta });
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


        [HttpGet("duracionAbrazos")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var respuesta = negAsignacion.devolverDuracionesAbrazos();

                return ApiResults.Success(new { estadisticaDuraciones = respuesta });
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

        [HttpGet("listarAsignacionesHoyVoluntaria/{idVoluntaria}")]
        public IActionResult GetAsignaciones(int idVoluntaria)
        {
            try
            {
                var respuesta = negAsignacion.listarAsignacionesHoyVoluntaria(idVoluntaria);
                return ApiResults.Success(new { listadoAsignaciones = respuesta });
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

        [HttpPost("registrarDetalleAsignacion")]
        public IActionResult Post(List<RequestDetalleAsignacion> request)
        {
            try
            {
                var respuesta = negAsignacion.registrarDetalleAsignacion(request);
                return ApiResults.Success(new { respuesta = respuesta });
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

        [HttpPost("generar")]
        public IActionResult Post()
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganaciones();
                return ApiResults.Success(new {listadoAsignaciones = respuesta});
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


        [HttpPost("generarTarea")]
        public IActionResult PostAsignacion(RequestAsignacionTarea request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTarea(request);
                return ApiResults.Success(new { listadoAsignaciones = respuesta });
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

        [HttpPost("generarTareas")]
        public IActionResult PostAsignaciones(RequestAsignacionTareas request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTareas(request);
                return ApiResults.Success(new { listadoAsignaciones = respuesta });
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

        [HttpPost("iniciarAbrazo/{idAsignacion}")]
        public IActionResult PostIniciarAbrazado(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.registrarInicioAsignacionAbrazo(idAsignacion);
                return ApiResults.Success(new { respuesta = respuesta });
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

        [HttpDelete("id/{idAsignacion}")]
        public IActionResult DeleteAsignacion(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.eliminarAsignacion(idAsignacion);
                return ApiResults.Success(new { respuesta });
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

        [HttpPut("id/{idAsignacion}")]
        public IActionResult PutModificarAsignacion(int idAsignacion, ASIGNACION datos)
        {
            try
            {
                var respuesta = negAsignacion.modificarAsignacion(idAsignacion, datos);
                return ApiResults.Success(new { respuesta });
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

        [HttpPost("finalizarAbrazo")]
        public IActionResult PostFinalizarAbrazado(ResquestFinalizarAbrazo request)
        {
            try
            {
                var respuesta = negAsignacion.registrarFinAsignacionAbrazo(request.idAsignacion, request.comentario);
                return ApiResults.Success(new { respuesta = respuesta });
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

        /// <summary>Cierra abrazos colgados: iniciados antes de hoy (AR) sin finalizar. Bebé → Sin abrazar, voluntaria → Activa.</summary>
        [HttpPost("resetearAbrazosColgados")]
        public IActionResult PostResetearAbrazosColgados()
        {
            try
            {
                var cantidad = negAsignacion.ResetearAbrazosBebeColgadosAntesDeHoy();
                return ApiResults.Success(new { cantidad });
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
