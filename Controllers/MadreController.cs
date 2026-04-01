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
    public class MadreController : ControllerBase
    {
        // GET: MadreController
        public readonly NegMadres neg_Madres;
        // GET: api/<ErroresController>
        public MadreController()
        {
            neg_Madres = new NegMadres();
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listadoMadres = neg_Madres.listarMadres();
                return Ok(new { listadoMadres = listadoMadres });
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

        [HttpGet("estadisticaLocalidades")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var resultado = neg_Madres.devolverEstadisticasLocalidades();
                return Ok(new { resultado = resultado });
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

        [HttpGet("id/{Id}")]
        public IActionResult GetMadre(int Id)
        {
            try
            {
                var madreDni = neg_Madres.consultarMadre(Id);
                return Ok(new { madre = madreDni });
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

        [HttpPost]
        public IActionResult Post(MADRE Madre)
        {
            try
            {
                var resultado = neg_Madres.registrarMadre(Madre);

                if (resultado.Exito)
                    return Ok(true);

                return StatusCode(500, resultado.Errores);
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
        public IActionResult Put(MADRE madre, int Id)
        {
            try
            {
                var resultado = neg_Madres.modificarMadre(madre,Id);
                return Ok(resultado);
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

        [HttpGet("estadisticaEdadesMadre")]
        public IActionResult GetEstadisticasEdadesMadres()
        {
            try
            {
                var resultado = neg_Madres.devolverEstadisticasEdadesMadres();
                return Ok(new { resultado = resultado });
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


    }
}
