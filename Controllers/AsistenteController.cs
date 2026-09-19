using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Security.Claims;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenteController(INegAsistente negAsistente) : ControllerBase
    {
        private static int ObtenerDniAutenticado(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var dni))
                throw new ApplicationException("No se pudo identificar al usuario autenticado.");
            return dni;
        }

        /// <summary>Indica si el asistente tiene OpenAI configurado (sin exponer la clave).</summary>
        [HttpGet("estado")]
        public IActionResult GetEstado()
        {
            try
            {
                return ApiResults.Success(negAsistente.ObtenerEstado());
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Interpreta una pregunta de la coordinadora y consulta el sistema (solo lectura).</summary>
        [HttpPost("preguntar")]
        [RequestTimeout("AsistenteLargo")]
        public async Task<IActionResult> Preguntar([FromBody] AsistentePreguntaRequest request)
        {
            try
            {
                var dni = ObtenerDniAutenticado(User);
                var resultado = await negAsistente.Preguntar(dni, request, HttpContext.RequestAborted);
                return ApiResults.Success(resultado);
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, new ApiResponse
                {
                    message = "La consulta al asistente tardó demasiado. Probá de nuevo o acortá la pregunta.",
                    errors = new List<string> { "Timeout del asistente." }
                });
            }
            catch (ForbiddenException ex)
            {
                return ApiResults.Forbidden(ex.Message);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError(ex.Message);
            }
        }
    }
}
