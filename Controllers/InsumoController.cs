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
                return Ok(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("consultaMovimientos")]
        public IActionResult GetMovimientos(RequestMovimiento? movimientoFiltro)
        {
            try
            {
                var resultado = negInsumos.obtenerMovimientos(movimientoFiltro);
                return Ok(new { listadoMovimientos = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        [HttpGet("proveedores")]
        public IActionResult GetProveedores()
        {
            try
            {
                var resultado = negInsumos.obtenerProveedores();
                return Ok(new { listadoDeProveedores = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("estadisticaInsumoCantidad")]
        public IActionResult GetEstadisticasInsumos()
        {
            try
            {
                var resultado=negInsumos.obtenerEstadisticaInsumo();
                return Ok(new { listadoInsumo = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("registrarMovimiento")]
        public IActionResult Post(MOVIMIENTOSTOCK movimiento)
        {
            try
            {
                var resultado = negInsumos.registrarMovimientoInsumos(movimiento);
                return Ok(new { resultado = resultado });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
