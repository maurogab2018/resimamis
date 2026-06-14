using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MadreController : ControllerBase
    {
        // GET: MadreController
        public readonly NegMadres neg_Madres;
        // GET: api/<ErroresController>
        public MadreController()
        {
            neg_Madres = new NegMadres();
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listadoMadres = neg_Madres.listarMadres();
                return ApiResults.Success(new { listadoMadres = listadoMadres });
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }

        [HttpGet("estadisticaLocalidades")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var resultado = neg_Madres.devolverEstadisticasLocalidades();
                return ApiResults.Success(new { resultado = resultado });
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }

        [HttpGet("id/{Id}")]
        public IActionResult GetMadre(int Id)
        {
            try
            {
                var madreDni = neg_Madres.consultarMadre(Id);
                return ApiResults.Success(new { madre = madreDni });
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public IActionResult Post(MADRE Madre)
        {
            try
            {
                var resultado = neg_Madres.registrarMadre(Madre);

                if (resultado.Exito)
                    return ApiResults.Success(true);

                return ApiResults.ValidationError(resultado.Errores);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }

        [HttpPut("id/{Id}")]
        public IActionResult Put(MADRE madre, int Id)
        {
            try
            {
                var resultado = neg_Madres.modificarMadre(madre, Id, out var madreActualizada);

                if (!resultado.Exito)
                    return ApiResults.ValidationError(resultado.Errores);

                return ApiResults.Success(madreActualizada);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }

        [HttpPost("delete")]
        public IActionResult Delete(int idMadre)
        {
            try
            {
                var ok = neg_Madres.eliminarMadre(idMadre);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
        }

        [HttpGet("estadisticaEdadesMadre")]
        public IActionResult GetEstadisticasEdadesMadres()
        {
            try
            {
                var resultado = neg_Madres.devolverEstadisticasEdadesMadres();
                return ApiResults.Success(new { resultado = resultado });
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }

        }


    }
}
