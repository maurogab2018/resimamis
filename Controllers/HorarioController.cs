using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HorarioController : ControllerBase
    {
        public readonly NegHorariosVoluntaria negHorariosVoluntaria;
        public HorarioController()
        {
            negHorariosVoluntaria = new NegHorariosVoluntaria();  
        }
        [HttpGet("dias")]

        public IActionResult GetDias()
        {
            try
            {
                var respuesta = negHorariosVoluntaria.obtenerDias();
                return Ok(new {listadoDias = respuesta });
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
        public IActionResult Post(List<HorarioVoluntaria> horarioVoluntarias)
        {
            try
            {
                var respuesta = negHorariosVoluntaria.registrarHoraraioVoluntaria(horarioVoluntarias);
                return Ok(respuesta);
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
