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
    public class BebeController : ControllerBase
    {
        public readonly NegBebes neg_Bebes;
        public BebeController()
        {
            neg_Bebes = new NegBebes();
        }
        [HttpGet]
        public IActionResult Get()
        {
            var lista = neg_Bebes.listarBebes();
            return ApiResults.Success(new { ListadoBebes = lista });
        }

        [HttpGet("listarSalas")]
        public IActionResult ListarSalas()
        {
            var lista = neg_Bebes.listarSalas();
            return ApiResults.Success(new { ListadoSalas = lista });
        }

        [HttpGet("id/{Dni}")]
        public IActionResult Get(int Dni)
        {
            try
            {
                var bebeDni = neg_Bebes.consultarBebe(Dni);
                return ApiResults.Success(new { bebe = bebeDni });

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

        /// <summary>Bebés disponibles para abrazar hoy (estado Sin abrazar; sin abrazo ya iniciado en el día).</summary>
        [HttpGet("disponibles-abrazo")]
        [HttpGet("abrazar")]
        public IActionResult GetBebesDisponiblesParaAbrazar()
        {
            try
            {
                var listado = neg_Bebes.listarBebesAbrazar();
                return ApiResults.Success(new
                {
                    listadoBebesDisponiblesParaAbrazo = listado,
                    cantidad = listado.Count,
                    bebe = listado
                });
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

        [HttpPost]
        public IActionResult post(BEBE bebe)
        {
            try
            {
                var respuesta = neg_Bebes.registrarBebe(bebe);
                return ApiResults.Success(new { Respuesta = respuesta });
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


        [HttpPut]
        public IActionResult Put(BEBE bebe)
        {
            try
            {
                var respuesta=neg_Bebes.modificarBebe(bebe);
                return ApiResults.Success(respuesta);    
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

        [HttpPost("delete")]
        public IActionResult Delete(int idBebe)
        {
            try
            {
                var ok = neg_Bebes.eliminarBebe(idBebe);
                return ApiResults.Success(new { respuesta = ok });
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
