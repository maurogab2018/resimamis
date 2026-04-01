using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("consultar/{idAsignacion}")]
        public IActionResult GetId(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.listarAsignacionesHoy();
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("listarCantidadAsignacionesPorDia")]
        public IActionResult GetCantidadAsignaciones(/*RequestEstadisticaCantidadAsignaciones request*/)
        {
            try
            {
                var respuesta = negAsignacion.devolverEstadisticaCantidadAsignaciones(/*request.fechaInicio,request.fechaFin*/);
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("duracionAbrazos")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var respuesta = negAsignacion.devolverDuracionesAbrazos();

                return Ok(new { estadisticaDuraciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("listarAsignacionesHoyVoluntaria/{idVoluntaria}")]
        public IActionResult GetAsignaciones(int idVoluntaria)
        {
            try
            {
                var respuesta = negAsignacion.listarAsignacionesHoyVoluntaria(idVoluntaria);
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("registrarDetalleAsignacion")]
        public IActionResult Post(List<RequestDetalleAsignacion> request)
        {
            try
            {
                var respuesta = negAsignacion.registrarDetalleAsignacion(request);
                return Ok(new { respuesta = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("generar")]
        public IActionResult Post()
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganaciones();
                return Ok(new {listadoAsignaciones = respuesta});
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new{ mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500,new { mensaje = ex.Message });
            }
        }


        [HttpPost("generarTarea")]
        public IActionResult PostAsignacion(RequestAsignacionTarea request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTarea(request);
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpPost("generarTareas")]
        public IActionResult PostAsignaciones(RequestAsignacionTareas request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTareas(request);
                return Ok(new { listadoAsignaciones = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpPost("iniciarAbrazo/{idAsignacion}")]
        public IActionResult PostIniciarAbrazado(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.registrarInicioAsignacionAbrazo(idAsignacion);
                return Ok(new { respuesta = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("finalizarAbrazo")]
        public IActionResult PostFinalizarAbrazado(ResquestFinalizarAbrazo request)
        {
            try
            {
                var respuesta = negAsignacion.registrarFinAsignacionAbrazo(request.idAsignacion, request.comentario);
                return Ok(new { respuesta = respuesta });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }
}
