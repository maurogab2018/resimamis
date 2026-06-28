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
    public class SalaController(INegSalas negSalas) : ControllerBase
    {
        /// <summary>Catálogo completo de salas (ABM).</summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listado = negSalas.listarSalas();
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

        /// <summary>Salas activas (combos, alta de bebés).</summary>
        [HttpGet("activas")]
        public IActionResult GetActivas()
        {
            try
            {
                var listado = negSalas.listarSalasActivas();
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

        [HttpGet("id/{idSala}")]
        public IActionResult GetById(int idSala)
        {
            try
            {
                var sala = negSalas.consultarSala(idSala);
                return ApiResults.Success(sala);
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
        public IActionResult Post(SALA sala)
        {
            try
            {
                var ok = negSalas.registrarSala(sala);
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

        [HttpPut("id/{idSala}")]
        public IActionResult Put(int idSala, SALA sala)
        {
            try
            {
                var ok = negSalas.modificarSala(idSala, sala);
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
        public IActionResult Delete(int idSala)
        {
            try
            {
                var ok = negSalas.eliminarSala(idSala);
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
