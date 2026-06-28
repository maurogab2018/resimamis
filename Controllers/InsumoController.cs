using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InsumoController(INegInsumos negInsumos) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var resultado = negInsumos.obtenerInsumos();
                return ApiResults.Success(resultado);
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

        [HttpGet("id/{idInsumo}")]
        public IActionResult GetById(int idInsumo)
        {
            try
            {
                var insumo = negInsumos.obtenerInsumoPorId(idInsumo);
                if (insumo == null)
                    return ApiResults.NotFound("Insumo inexistente o no disponible.");
                return ApiResults.Success(insumo);
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
        public IActionResult Post(INSUMO insumo)
        {
            try
            {
                var creado = negInsumos.registrarInsumo(insumo);
                return ApiResults.Success(creado);
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

        [HttpPut("id/{idInsumo}")]
        public IActionResult Put(int idInsumo, INSUMO insumo)
        {
            try
            {
                var ok = negInsumos.modificarInsumo(idInsumo, insumo);
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
        public IActionResult Delete(int idInsumo)
        {
            try
            {
                var ok = negInsumos.eliminarInsumo(idInsumo);
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

        [HttpGet("movimiento/id/{idMovimiento}")]
        public IActionResult GetMovimientoPorId(int idMovimiento)
        {
            try
            {
                var resultado = negInsumos.obtenerMovimientoPorId(idMovimiento);
                return ApiResults.Success(resultado);
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

        [HttpPost("consultaMovimientos")]
        public IActionResult GetMovimientos(RequestMovimiento? movimientoFiltro)
        {
            try
            {
                var resultado = negInsumos.obtenerMovimientos(movimientoFiltro);
                return ApiResults.Success(resultado);
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



        [HttpGet("proveedores")]
        public IActionResult GetProveedores()
        {
            try
            {
                var resultado = negInsumos.obtenerProveedores();
                return ApiResults.Success(resultado);
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


        [HttpGet("estadisticaInsumoCantidad")]
        public IActionResult GetEstadisticasInsumos()
        {
            try
            {
                var resultado=negInsumos.obtenerEstadisticaInsumo();
                return ApiResults.Success(resultado);
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

        [HttpPost("registrarMovimiento")]
        public IActionResult Post(MOVIMIENTOSTOCK movimiento)
        {
            try
            {
                var resultado = negInsumos.registrarMovimientoInsumos(movimiento);
                return ApiResults.Success(resultado);
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
