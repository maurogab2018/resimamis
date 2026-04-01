using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        public readonly NegUsuarios neg_Usuario;
        // GET: api/<ErroresController>
        public UsuarioController()
        {
            neg_Usuario = new NegUsuarios();
        }

        [HttpPost]
        public IActionResult Post(USUARIO Usuario)
        {
            try
            {
                var registroUsuario = neg_Usuario.RegistrarUsuario(Usuario);
                return Ok(registroUsuario);
            }
            catch (ApplicationException exApp)
            {
                return BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [HttpPost("login")]
        public IActionResult Put(RequestLogin Usuario)
        {
            try
            {
                var respuestaLogin = neg_Usuario.Loguear(Usuario);
                return Ok(respuestaLogin);
            }
            catch (ApplicationException exApp)
            {
                return BadRequest(new { message=exApp.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }
    }
}
