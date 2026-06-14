using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;
using System.Security.Claims;

namespace ResimamisBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        public readonly NegUsuarios neg_Usuario;

        public UsuarioController()
        {
            neg_Usuario = new NegUsuarios();
        }

        private int ObtenerDniAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var dni))
                throw new ApplicationException("No se pudo identificar al usuario autenticado.");
            return dni;
        }

        /// <summary>Registro de usuario (solo rol Administrativa). Asociar a una voluntaria sin usuario.</summary>
        [Authorize]
        [HttpPost]
        public IActionResult Post(USUARIO Usuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var registroUsuario = neg_Usuario.RegistrarUsuario(dni, Usuario);
                return ApiResults.Success(new { respuesta = registroUsuario });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost("login")]
        public IActionResult Login(RequestLogin Usuario)
        {
            try
            {
                var respuestaLogin = neg_Usuario.Loguear(Usuario);
                return ApiResults.Success(respuestaLogin);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Listado de usuarios activos (solo Administrativa): dni, voluntaria, fecha creación.</summary>
        [Authorize]
        [HttpGet]
        public IActionResult GetListado()
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var listado = neg_Usuario.ListarUsuarios(dni);
                return ApiResults.Success(new { listadoUsuarios = listado });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Voluntarias sin usuario asociado (solo Administrativa), para alta de usuario.</summary>
        [Authorize]
        [HttpGet("voluntarias-sin-usuario")]
        public IActionResult GetVoluntariasSinUsuario()
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var listado = neg_Usuario.ListarVoluntariasSinUsuario(dni);
                return ApiResults.Success(new { listadoVoluntariasSinUsuario = listado });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("id/{idUsuario}")]
        public IActionResult GetById(int idUsuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var usuario = neg_Usuario.ConsultarUsuarioPorId(idUsuario, dni);
                return ApiResults.Success(new { usuario });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Cambio de contraseña del usuario autenticado (requiere contraseña actual).</summary>
        [Authorize]
        [HttpPut("contrasena")]
        public IActionResult PutContrasena(RequestCambiarContrasena datos)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var ok = neg_Usuario.CambiarContrasena(dni, datos);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Modificar usuario (solo Administrativa): dni, voluntaria y opcionalmente contraseña.</summary>
        [Authorize]
        [HttpPut("id/{idUsuario}")]
        public IActionResult PutUsuario(int idUsuario, USUARIO usuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var ok = neg_Usuario.ModificarUsuario(dni, idUsuario, usuario);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        /// <summary>Baja lógica del usuario (estado Eliminado, ámbito Usuarios). Solo Administrativa.</summary>
        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(int idUsuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var ok = neg_Usuario.EliminarUsuario(dni, idUsuario);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }
    }
}
