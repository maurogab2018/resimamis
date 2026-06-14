using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VisitaController : ControllerBase
    {
        private readonly NegVisitas negVisitas;

        public VisitaController()
        {
            negVisitas = new NegVisitas();
        }

        /// <summary>Listado general de visitas activas (incluye datos del bebé).</summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listado = negVisitas.listarVisitas();
                return ApiResults.Success(new { listadoVisitas = listado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        /// <summary>Visitas activas de un bebé.</summary>
        [HttpGet("bebe/{idBebe}")]
        public IActionResult GetPorBebe(int idBebe)
        {
            try
            {
                var listado = negVisitas.listarVisitasPorBebe(idBebe);
                return ApiResults.Success(new { listadoVisitas = listado, idBebe });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        [HttpGet("id/{idVisita}")]
        public IActionResult GetById(int idVisita)
        {
            try
            {
                var visita = negVisitas.consultarVisita(idVisita);
                return ApiResults.Success(new { visita });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult Post(VISITA visita)
        {
            try
            {
                var creada = negVisitas.registrarVisita(visita);
                return ApiResults.Success(new { visita = creada });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        [HttpPut("id/{idVisita}")]
        public IActionResult Put(int idVisita, VISITA visita)
        {
            try
            {
                var ok = negVisitas.modificarVisita(idVisita, visita);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idVisita)
        {
            try
            {
                var ok = negVisitas.eliminarVisita(idVisita);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }
    }
}
