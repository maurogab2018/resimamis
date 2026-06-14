using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VoluntariaController : ControllerBase
    {
        public readonly NegVoluntaria negVoluntaria;
        public VoluntariaController()
        {
            negVoluntaria = new NegVoluntaria();
        }
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listadoVoluntaria = negVoluntaria.listarVoluntarias();
                return ApiResults.Success(listadoVoluntaria);
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

        [HttpGet("libres")]
        public IActionResult GetLibres()
        {
            try
            {
                var listadoVoluntariasLibres = negVoluntaria.listarVoluntariasLibres1();
                return ApiResults.Success(listadoVoluntariasLibres);
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
        [HttpGet("estados")]
        public IActionResult GetEstados()
        {
            try
            {
                var listadoEstadosVoluntarias = negVoluntaria.devolverEstadosVoluntarias();
                return ApiResults.Success(listadoEstadosVoluntarias.Select(e=> new {idEstado=e.idEstado,nombre=e.nombre,descripcion=e.descripcion,idAmbito=e.idAmbito}));
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

        [HttpGet("id/{Id}")]
        public IActionResult GetVoluntaria(int Id)
        {
            try
            {
                var voluntariaDni = negVoluntaria.consultarVoluntaria(Id);
                return ApiResults.Success(voluntariaDni);
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

        [HttpPut("id/{Id}")]
        public IActionResult Put(VOLUNTARIA voluntaria, int Id)
        {
            try
            {
                var resultado = negVoluntaria.modificarVoluntaria(voluntaria,Id);
                return ApiResults.Success(resultado);
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


        [HttpPost("delete")]
        public IActionResult Delete(int idVoluntaria)
        {
            try
            {
                var registroVoluntaria = negVoluntaria.eliminarVoluntaria(idVoluntaria);
                return ApiResults.Success(registroVoluntaria);
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
        public IActionResult Post(VOLUNTARIA voluntaria)
        {
            try
            {
                var registroVoluntaria = negVoluntaria.registrarVoluntaria(voluntaria);
                return ApiResults.Success(registroVoluntaria);
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
