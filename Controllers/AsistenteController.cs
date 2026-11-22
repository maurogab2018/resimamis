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
                int? dni = null;
                try { dni = ObtenerDniAutenticado(User); } catch (ApplicationException) { }
                return ApiResults.Success(negAsistente.ObtenerEstado(dni));
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
            catch (LimiteAsistenteException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponse
                {
                    message = ex.Message,
                    errors = new List<string> { ex.Message }
                });
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

        /// <summary>Calificación opcional de una respuesta propia: 1 = bien, -1 = mal, 0 = quitar el voto.</summary>
        [HttpPost("calificar")]
        public IActionResult Calificar([FromBody] AsistenteCalificarRequest request)
        {
            try
            {
                negAsistente.Calificar(ObtenerDniAutenticado(User), request);
                return ApiResults.Success(true);
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

        /// <summary>Últimas preguntas al asistente (solo coordinadoras). soloMalas=true: 👎 o que no terminaron bien.</summary>
        [HttpGet("registros")]
        public IActionResult GetRegistros([FromQuery] bool soloMalas = false, [FromQuery] int limite = 100)
        {
            try
            {
                return ApiResults.Success(negAsistente.ObtenerRegistros(ObtenerDniAutenticado(User), soloMalas, limite));
            }
            catch (ForbiddenException ex)
            {
                return ApiResults.Forbidden(ex.Message);
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
