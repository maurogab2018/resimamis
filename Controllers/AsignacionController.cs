using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using System.Security.Claims;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsignacionController: ControllerBase
    {
        public readonly NegAsignacion negAsignacion;

        public AsignacionController()
        {
            negAsignacion = new NegAsignacion();
        }

        private static int ObtenerDniAutenticado(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var dni))
                throw new ApplicationException("No se pudo identificar al usuario autenticado.");
            return dni;
        }

        /// <summary>Listado de asignaciones del día (solo Coordinadora).</summary>
        [HttpGet("listarAsignacionesHoy")]
        public IActionResult Get()
        {
            try
            {
                var dni = ObtenerDniAutenticado(User);
                var respuesta = negAsignacion.listarAsignacionesHoy(dni);
                return ApiResults.Success(respuesta);
            }
            catch (ForbiddenException ex)
            {
                return ApiResults.Forbidden(ex.Message);
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

        [HttpGet("consultar/{idAsignacion}")]
        public IActionResult GetId(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.consultarAsignacionPorId(idAsignacion);
                return ApiResults.Success(respuesta);
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


        [HttpGet("listarCantidadAsignacionesPorDia")]
        public IActionResult GetCantidadAsignaciones(/*RequestEstadisticaCantidadAsignaciones request*/)
        {
            try
            {
                var respuesta = negAsignacion.devolverEstadisticaCantidadAsignaciones(/*request.fechaInicio,request.fechaFin*/);
                return ApiResults.Success(respuesta);
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


        [HttpGet("duracionAbrazos")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var respuesta = negAsignacion.devolverDuracionesAbrazos();

                return ApiResults.Success(respuesta);
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

        [HttpGet("listarAsignacionesHoyVoluntaria/{idVoluntaria}")]
        public IActionResult GetAsignaciones(int idVoluntaria)
        {
            try
            {
                var respuesta = negAsignacion.listarAsignacionesHoyVoluntaria(idVoluntaria);
                return ApiResults.Success(respuesta);
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

        [HttpPost("registrarDetalleAsignacion")]
        public IActionResult Post(List<RequestDetalleAsignacion> request)
        {
            try
            {
                var respuesta = negAsignacion.registrarDetalleAsignacion(request);
                return ApiResults.Success(respuesta);
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

        [HttpPost("generar")]
        public IActionResult Post()
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganaciones();
                return ApiResults.Success(respuesta);
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


        [HttpPost("generarTarea")]
        public IActionResult PostAsignacion(RequestAsignacionTarea request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTarea(request);
                return ApiResults.Success(respuesta);
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

        [HttpPost("generarTareas")]
        public IActionResult PostAsignaciones(RequestAsignacionTareas request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsignacionesSeleccion(request);
                return ApiResults.Success(respuesta);
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

        [HttpPost("generarTareasPorId")]
        public IActionResult PostAsignacionesPorIdTarea(RequestAsignacionTareas request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsiganacionTareasPorId(request);
                return ApiResults.Success(respuesta);
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

        [HttpPost("iniciarAbrazo/{idAsignacion}")]
        public IActionResult PostIniciarAbrazado(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.registrarInicioAsignacionAbrazo(idAsignacion);
                return ApiResults.Success(respuesta);
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

        [HttpDelete("id/{idAsignacion}")]
        public IActionResult DeleteAsignacion(int idAsignacion)
        {
            try
            {
                var respuesta = negAsignacion.eliminarAsignacion(idAsignacion);
                return ApiResults.Success(respuesta);
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

        [HttpPut("id/{idAsignacion}")]
        public IActionResult PutModificarAsignacion(int idAsignacion, ASIGNACION datos)
        {
            try
            {
                var respuesta = negAsignacion.modificarAsignacion(idAsignacion, datos);
                return ApiResults.Success(respuesta);
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

        [HttpPost("finalizarAbrazo")]
        public IActionResult PostFinalizarAbrazado(ResquestFinalizarAbrazo request)
        {
            try
            {
                var respuesta = negAsignacion.registrarFinAsignacionAbrazo(request.idAsignacion, request.comentario);
                return ApiResults.Success(respuesta);
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

        [HttpPost("resetearAbrazosColgados")]
        public IActionResult PostResetearAbrazosColgados()
        {
            try
            {
                var cantidad = negAsignacion.ResetearAbrazosBebeColgadosAntesDeHoy();
                return ApiResults.Success(cantidad);
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
