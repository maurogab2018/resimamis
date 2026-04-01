using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BebeController : ControllerBase
    {
        public readonly NegBebes neg_Bebes;
        public BebeController()
        {
            neg_Bebes = new NegBebes();
        }
        [HttpGet]
        public IActionResult Get()
        {
            var lista = neg_Bebes.listarBebes();
            return Ok(new { ListadoBebes = lista });
        }

        [HttpGet("listarSalas")]
        public IActionResult ListarSalas()
        {
            var lista = neg_Bebes.listarSalas();
            return Ok(new { ListadoSalas = lista });
        }

        [HttpGet("id/{Dni}")]
        public IActionResult Get(int Dni)
        {
            try
            {
                var bebeDni = neg_Bebes.consultarBebe(Dni);
                return Ok(new { bebe = bebeDni });

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

        [HttpGet("abrazar")]
        public IActionResult GetAbrazar()
        {
            try
            {
                var bebeDni = neg_Bebes.listarBebesAbrazar();
                return Ok(new { bebe = bebeDni });
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

        [HttpPost]
        public IActionResult post(BEBE bebe)
        {
            try
            {
                var respuesta = neg_Bebes.registrarBebe(bebe);
                return Ok(new { Respuesta = respuesta });
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


        [HttpPut]
        public IActionResult Put(BEBE bebe)
        {
            try
            {
                var respuesta=neg_Bebes.modificarBebe(bebe);
                return Ok(respuesta);    
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
