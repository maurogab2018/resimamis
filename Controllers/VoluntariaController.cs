using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
  //  [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VoluntariaController : ControllerBase
    {
        public readonly NegVoluntaria negVoluntaria;
        public VoluntariaController()
        {
            negVoluntaria = new NegVoluntaria();
        }
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listadoVoluntaria = negVoluntaria.listarVoluntarias();
                return Ok(new { listadoVoluntaria = listadoVoluntaria });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("libres")]
        public IActionResult GetLibres()
        {
            try
            {
                var listadoVoluntariasLibres = negVoluntaria.listarVoluntariasLibres1();
                return Ok(new { listadoVoluntariasLibres = listadoVoluntariasLibres });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("estados")]
        public IActionResult GetEstados()
        {
            try
            {
                var listadoEstadosVoluntarias = negVoluntaria.devolverEstadosVoluntarias();
                return Ok(new { listadoEstadosVoluntarias = listadoEstadosVoluntarias.Select(e=> new {idEstado=e.idEstado,nombre=e.nombre,descripcion=e.descripcion,idAmbito=e.idAmbito}) });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("id/{Id}")]
        public IActionResult GetVoluntaria(int Id)
        {
            try
            {
                var voluntariaDni = negVoluntaria.consultarVoluntaria(Id);
                return Ok(new { voluntaria = voluntariaDni });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [HttpPut("id/{Id}")]
        public IActionResult Put(VOLUNTARIA voluntaria, int Id)
        {
            try
            {
                var resultado = negVoluntaria.modificarVoluntaria(voluntaria,Id);
                return Ok (new{ respuesta = resultado });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpPost("delete")]
        public IActionResult Delete(int idVoluntaria)
        {
            try
            {
                var registroVoluntaria = negVoluntaria.eliminarVoluntaria(idVoluntaria);
                return Ok(new { respuesta = registroVoluntaria });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost]
        public IActionResult Post(VOLUNTARIA voluntaria)
        {
            try
            {
                var registroVoluntaria = negVoluntaria.registrarVoluntaria(voluntaria);
                return Ok(new { respuesta=registroVoluntaria });
            }
            catch (ApplicationException exa)
            {
                return BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



    }
}
