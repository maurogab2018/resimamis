using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

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
                return Ok(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);  
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }    
        }

        [HttpGet("hoy")]
        public IActionResult GetAsistenciasVoluntarias()
        {
            try
            {
                var resultado = negAsistencia.consultarAsistenciasFechahoy();
                return Ok(resultado);
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

        [HttpGet("historicas/{IdVoluntaria}")]
        public IActionResult GetAsistenciasVoluntariasHistoricas(int IdVoluntaria)
        {
            try
            {
                var resultado = negAsistencia.consultarAsistenciasVoluntaria(IdVoluntaria);
                return Ok(resultado);
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

        [HttpPost("entrada/{IdVoluntaria}")]
        public IActionResult Post(int IdVoluntaria)
        {
            try
            {
                var respuesta= negAsistencia.registrarAsistencia(IdVoluntaria);
                return Ok(new {respuesta= respuesta});
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

        [HttpPost("salida/{IdVoluntaria}")]
        public IActionResult PostSalida(int IdVoluntaria)
        {
            try
            {
                var resultado = negAsistencia.registrarAsistenciaSalida(IdVoluntaria);
                return Ok(resultado);
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
