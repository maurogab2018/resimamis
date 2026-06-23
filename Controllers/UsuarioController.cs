using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
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

        /// <summary>Registro de usuario (solo rol Coordinadora). Asociar a una voluntaria sin usuario.</summary>
        [Authorize]
        [HttpPost]
        public IActionResult Post(USUARIO Usuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var registroUsuario = neg_Usuario.RegistrarUsuario(dni, Usuario);
                return ApiResults.Success(registroUsuario);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
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
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Listado de usuarios activos (solo Coordinadora): dni, voluntaria, fecha creación.</summary>
        [Authorize]
        [HttpGet]
        public IActionResult GetListado()
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var listado = neg_Usuario.ListarUsuarios(dni);
                return ApiResults.Success(listado);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Voluntarias sin usuario asociado (solo Coordinadora), para alta de usuario.</summary>
        [Authorize]
        [HttpGet("voluntarias-sin-usuario")]
        public IActionResult GetVoluntariasSinUsuario()
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var listado = neg_Usuario.ListarVoluntariasSinUsuario(dni);
                return ApiResults.Success(listado);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
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
                return ApiResults.Success(usuario);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
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
                return ApiResults.Success(ok);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Modificar usuario (solo Coordinadora): dni, voluntaria y opcionalmente contraseña.</summary>
        [Authorize]
        [HttpPut("id/{idUsuario}")]
        public IActionResult PutUsuario(int idUsuario, USUARIO usuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var ok = neg_Usuario.ModificarUsuario(dni, idUsuario, usuario);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Baja lógica del usuario (estado Eliminado, ámbito Usuarios). Solo Coordinadora.</summary>
        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(int idUsuario)
        {
            try
            {
                var dni = ObtenerDniAutenticado();
                var ok = neg_Usuario.EliminarUsuario(dni, idUsuario);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException exApp)
            {
                return ApiResults.NotFound(exApp.Message);
            }
            catch (UnauthorizedException exApp)
            {
                return ApiResults.Unauthorized(exApp.Message);
            }
            catch (ForbiddenException exApp)
            {
                return ApiResults.Forbidden(exApp.Message);
            }
            catch (ConflictException exApp)
            {
                return ApiResults.Conflict(exApp.Message);
            }
            catch (ApplicationException exApp)
            {
                return ApiResults.BadRequest(exApp.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
