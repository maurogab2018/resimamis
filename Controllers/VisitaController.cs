using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Text.Json;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VisitaController(INegVisitas negVisitas) : ControllerBase
    {
        private static readonly JsonSerializerOptions VisitaJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static VISITA LeerVisitaDesdeBody(JsonElement body)
        {
            var payload = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("data", out var data)
                ? data
                : body;

            return JsonSerializer.Deserialize<VISITA>(payload.GetRawText(), VisitaJsonOptions)
                ?? throw new ApplicationException("Visita inválida.");
        }

        /// <summary>Listado general de visitas activas (incluye datos del bebé).</summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listado = negVisitas.listarVisitas();
                return ApiResults.Success(listado);
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
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Visitas activas de un bebé.</summary>
        [HttpGet("bebe/{idBebe}")]
        public IActionResult GetPorBebe(int idBebe)
        {
            try
            {
                var listado = negVisitas.listarVisitasPorBebe(idBebe);
                return ApiResults.Success(listado);
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
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("id/{idVisita}")]
        public IActionResult GetById(int idVisita)
        {
            try
            {
                var visita = negVisitas.consultarVisita(idVisita);
                return ApiResults.Success(visita);
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
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost]
        public IActionResult Post([FromBody] JsonElement body)
        {
            try
            {
                var visita = LeerVisitaDesdeBody(body);
                var creada = negVisitas.registrarVisita(visita);
                return ApiResults.Success(creada);
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
                return ApiResults.InternalServerError();
            }
        }

        [HttpPut("id/{idVisita}")]
        public IActionResult Put(int idVisita, [FromBody] JsonElement body)
        {
            try
            {
                var visita = LeerVisitaDesdeBody(body);
                var ok = negVisitas.modificarVisita(idVisita, visita);
                return ApiResults.Success(ok);
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
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idVisita)
        {
            try
            {
                var ok = negVisitas.eliminarVisita(idVisita);
                return ApiResults.Success(ok);
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
                return ApiResults.InternalServerError();
            }
        }
    }
}
