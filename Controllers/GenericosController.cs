using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GenericosController : ControllerBase
    {
        public readonly NegGenericos negGenericos;
        public GenericosController()
        {
            negGenericos = new NegGenericos();
        }

        [HttpGet("localidades")]
        public IActionResult Get(int Dni)
        {
            try
            {
                var localidades= negGenericos.obtenerLocalidades();
                return Ok(new { localidades = localidades });
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
