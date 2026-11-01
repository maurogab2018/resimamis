using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BajasController(INegRegistroActividad negRegistroActividad) : ControllerBase
    {
        /// <summary>
        /// Historial de madres, bebés y voluntarias dados de baja, más recientes primero. Query
        /// opcional <c>tipo</c>: Madre, Bebe o Voluntaria. El detalle de cada registro se consulta
        /// con el GET por id del recurso (api/Madre/id/{id}, api/Bebe/id/{id}, api/Voluntaria/id/{id}).
        /// </summary>
        [HttpGet("historial")]
        public IActionResult GetHistorial([FromQuery] string? tipo)
        {
            try
            {
                var resultado = negRegistroActividad.ListarHistorialBajas(tipo);
                return ApiResults.Success(resultado);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
