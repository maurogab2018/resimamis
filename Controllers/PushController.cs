using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    /// <summary>Notificaciones push del navegador (Web Push) de la voluntaria logueada.</summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PushController(INegNotificaciones negNotificaciones) : ControllerBase
    {
        private int Dni()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var dni))
                throw new UnauthorizedException("No se pudo identificar al usuario autenticado.");
            return dni;
        }

        private IActionResult Ejecutar(Func<object?> accion)
        {
            try
            {
                return ApiResults.Success(accion());
            }
            catch (UnauthorizedException ex) { return ApiResults.Unauthorized(ex.Message); }
            catch (ForbiddenException ex) { return ApiResults.Forbidden(ex.Message); }
            catch (NotFoundException ex) { return ApiResults.NotFound(ex.Message); }
            catch (ApplicationException ex) { return ApiResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                Console.WriteLine($"[push] error: {ex.Message}");
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Clave pública VAPID y si la voluntaria ya tiene dispositivos suscriptos.</summary>
        [HttpGet("estado")]
        public IActionResult Estado() => Ejecutar(() => new
        {
            clavePublica = negNotificaciones.ClavePublicaPush(),
            dispositivos = negNotificaciones.DispositivosConPush(Dni()),
        });

        [HttpPost("suscribir")]
        public IActionResult Suscribir([FromBody] RequestSuscripcionPush body) => Ejecutar(() =>
        {
            negNotificaciones.SuscribirPush(Dni(), body);
            return true;
        });

        [HttpPost("desuscribir")]
        public IActionResult Desuscribir([FromBody] RequestDesuscripcionPush body) => Ejecutar(() =>
        {
            negNotificaciones.DesuscribirPush(Dni(), body?.endpoint ?? "");
            return true;
        });

        /// <summary>Manda una notificación de prueba a los dispositivos de la voluntaria logueada.</summary>
        [HttpPost("probar")]
        public IActionResult Probar() => Ejecutar(() => new { enviados = negNotificaciones.ProbarPush(Dni()) });
    }
}
