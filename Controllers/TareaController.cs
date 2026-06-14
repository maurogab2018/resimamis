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
    public class TareaController : ControllerBase
    {
        private readonly NegTareas negTareas;

        public TareaController()
        {
            negTareas = new NegTareas();
        }

        /// <summary>Catálogo completo de tareas (ABM).</summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listado = negTareas.listarTareas();
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Tareas activas disponibles para asignar hoy (respeta esUnica y asignaciones sin finalizar).</summary>
        [HttpGet("disponibles")]
        public IActionResult GetDisponibles()
        {
            try
            {
                var listado = negTareas.listarTareasDisponiblesParaAsignar();
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("id/{idTarea}")]
        public IActionResult GetById(int idTarea)
        {
            try
            {
                var tarea = negTareas.consultarTarea(idTarea);
                return ApiResults.Success(tarea);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost]
        public IActionResult Post(TAREA tarea)
        {
            try
            {
                var ok = negTareas.registrarTarea(tarea);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPut("id/{idTarea}")]
        public IActionResult Put(int idTarea, TAREA tarea)
        {
            try
            {
                var ok = negTareas.modificarTarea(idTarea, tarea);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idTarea)
        {
            try
            {
                var ok = negTareas.eliminarTarea(idTarea);
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
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }
    }
}
