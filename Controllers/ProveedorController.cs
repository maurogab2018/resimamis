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
    public class ProveedorController(INegProveedores negProveedores) : ControllerBase
    {
        /// <summary>Catálogo completo de proveedores (ABM).</summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listado = negProveedores.listarProveedores();
                return ApiResults.Success(listado);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Proveedores activos (combos, movimientos de stock).</summary>
        [HttpGet("activos")]
        public IActionResult GetActivos()
        {
            try
            {
                var listado = negProveedores.listarProveedoresActivos();
                return ApiResults.Success(listado);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("id/{idProveedor}")]
        public IActionResult GetById(int idProveedor)
        {
            try
            {
                var proveedor = negProveedores.consultarProveedor(idProveedor);
                return ApiResults.Success(proveedor);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost]
        public IActionResult Post(PROVEEDOR proveedor)
        {
            try
            {
                var ok = negProveedores.registrarProveedor(proveedor);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPut("id/{idProveedor}")]
        public IActionResult Put(int idProveedor, PROVEEDOR proveedor)
        {
            try
            {
                var ok = negProveedores.modificarProveedor(idProveedor, proveedor);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idProveedor)
        {
            try
            {
                var ok = negProveedores.eliminarProveedor(idProveedor);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ConflictException ex)
            {
                return ApiResults.Conflict(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
