using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HorarioController(INegHorariosVoluntaria negHorariosVoluntaria) : ControllerBase
    {
        [HttpGet("dias")]

        public IActionResult GetDias()
        {
            try
            {
                var respuesta = negHorariosVoluntaria.obtenerDias();
                return ApiResults.Success(respuesta);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("voluntaria/{idVoluntaria}")]
        public IActionResult GetHorariosPorVoluntaria(int idVoluntaria)
        {
            try
            {
                var respuesta = negHorariosVoluntaria.obtenerHorariosPorVoluntaria(idVoluntaria);
                return ApiResults.Success(respuesta);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpDelete("voluntaria/{idHorarioVoluntaria}")]
        public IActionResult DeleteHorarioVoluntaria(int idHorarioVoluntaria)
        {
            try
            {
                var respuesta = negHorariosVoluntaria.eliminarHorarioVoluntaria(idHorarioVoluntaria);
                return ApiResults.Success(respuesta);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost]
        public IActionResult Post(List<HorarioVoluntaria> horarioVoluntarias)
        {
            try
            {
                var respuesta = negHorariosVoluntaria.registrarHoraraioVoluntaria(horarioVoluntarias);
                return ApiResults.Success(respuesta);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }

        [HttpPut("{idVoluntaria}")]
        public IActionResult Put(int idVoluntaria, [FromBody] List<HorarioVoluntaria> horarioVoluntarias)
        {
            try
            {
                var respuesta = negHorariosVoluntaria.reemplazarHorarios(idVoluntaria, horarioVoluntarias);
                return ApiResults.Success(respuesta);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

    }
}
