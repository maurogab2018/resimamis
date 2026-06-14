using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InsumoController : ControllerBase
    {
        public readonly NegInsumos negInsumos;
        public InsumoController()
        {
            negInsumos = new NegInsumos();
        }


        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var resultado = negInsumos.obtenerInsumos();
                return ApiResults.Success(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpGet("id/{idInsumo}")]
        public IActionResult GetById(int idInsumo)
        {
            try
            {
                var insumo = negInsumos.obtenerInsumoPorId(idInsumo);
                if (insumo == null)
                    return ApiResults.BadRequest("Insumo inexistente o no disponible para el ámbito Insumos.");
                return ApiResults.Success(new { insumo });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult Post(INSUMO insumo)
        {
            try
            {
                var creado = negInsumos.registrarInsumo(insumo);
                return ApiResults.Success(new { insumo = creado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPut("id/{idInsumo}")]
        public IActionResult Put(int idInsumo, INSUMO insumo)
        {
            try
            {
                var ok = negInsumos.modificarInsumo(idInsumo, insumo);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idInsumo)
        {
            try
            {
                var ok = negInsumos.eliminarInsumo(idInsumo);
                return ApiResults.Success(new { respuesta = ok });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost("consultaMovimientos")]
        public IActionResult GetMovimientos(RequestMovimiento? movimientoFiltro)
        {
            try
            {
                var resultado = negInsumos.obtenerMovimientos(movimientoFiltro);
                return ApiResults.Success(new { listadoMovimientos = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }



        [HttpGet("proveedores")]
        public IActionResult GetProveedores()
        {
            try
            {
                var resultado = negInsumos.obtenerProveedores();
                return ApiResults.Success(new { listadoDeProveedores = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }


        [HttpGet("estadisticaInsumoCantidad")]
        public IActionResult GetEstadisticasInsumos()
        {
            try
            {
                var resultado=negInsumos.obtenerEstadisticaInsumo();
                return ApiResults.Success(new { listadoInsumo = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

        [HttpPost("registrarMovimiento")]
        public IActionResult Post(MOVIMIENTOSTOCK movimiento)
        {
            try
            {
                var resultado = negInsumos.registrarMovimientoInsumos(movimiento);
                return ApiResults.Success(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.ServerError(ex.Message);
            }
        }

    }
}
