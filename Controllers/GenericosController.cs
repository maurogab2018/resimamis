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
                return ApiResults.Success(localidades);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("estadosCiviles")]
        public IActionResult GetEstadosCiviles()
        {
            try
            {
                var estadosCiviles = negGenericos.obtenerEstadosCiviles();
                return ApiResults.Success(estadosCiviles);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
