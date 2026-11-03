using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;
using System.Security.Claims;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AsignacionController(INegAsignacion negAsignacion) : ControllerBase
    {
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

        /// <summary>Historial de abrazos de un bebé (todas las asignaciones con ese idBebe, sin eliminadas).</summary>
        [HttpGet("abrazosHistoricos/{idBebe}")]
        public IActionResult GetAbrazosHistoricos(int idBebe)
        {
            try
            {
                var respuesta = negAsignacion.listarAbrazosHistoricos(idBebe);
                return ApiResults.Success(respuesta);
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


        /// <summary>Asigna un bebé a una voluntaria. Body: idVoluntaria + idTarea (id del bebé).</summary>
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

        /// <summary>Asigna una tarea del catálogo (tabla TAREA) a una voluntaria.</summary>
        [HttpPost("generarTareaCatalogo")]
        public IActionResult PostAsignacionTareaCatalogo(RequestAsignacionTarea request)
        {
            try
            {
                var respuesta = negAsignacion.generarAsignacionTareaCatalogo(request);
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

        /// <summary>Asigna bebés a voluntarias. idTareas = ids de bebé (BEBE.ID).</summary>
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

        /// <summary>Asigna tareas del catálogo a voluntarias. idTareas = ids de TAREA.</summary>
        [HttpPost("generarTareasPorId")]
        [HttpPost("generarTareasCatalogo")]
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
        public IActionResult PutModificarAsignacion(int idAsignacion, RespuestaAsignaciones datos)
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

        /// <summary>
        /// Estado del cierre diario automático: las últimas corridas (guardadas en la bitácora, una
        /// por día) y la próxima programada, en hora Argentina.
        /// </summary>
        [HttpGet("cierre-diario/estado")]
        public IActionResult GetEstadoCierreDiario([FromServices] ApplicationDbContext db)
        {
            static string? Ar(DateTime? utc) =>
                utc.HasValue ? NegConversorFecha.HoraArgentina(utc.Value).ToString("yyyy-MM-dd HH:mm:ss") : null;
            try
            {
                var ultimas = db.REGISTROACTIVIDAD
                    .Where(r => r.tipoMovimiento == TiposMovimientoActividad.EjecucionCierreDiario)
                    .OrderByDescending(r => r.fechaHora)
                    .Take(7)
                    .Select(r => new { r.fechaHora, cantidadCerrada = r.idRegistro, r.descripcion })
                    .ToList()
                    .Select(r => new { fecha = Ar(r.fechaHora), r.cantidadCerrada, r.descripcion });

                return ApiResults.Success(new
                {
                    proximaEjecucion = Ar(CierreAbrazosDiarioServicio.ProximaEjecucionUtc),
                    ultimasEjecuciones = ultimas
                });
            }
            catch (Exception)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>
        /// Rastro de los abrazos cerrados automáticamente (cierre diario de medianoche o botón de
        /// coordinación). Query opcional fechaDesde/fechaHasta; sin fechas, los últimos 7 días.
        /// </summary>
        [HttpGet("cierres-automaticos")]
        public IActionResult GetCierresAutomaticos(
            [FromQuery] string? fechaDesde,
            [FromQuery] string? fechaHasta,
            [FromServices] INegRegistroActividad negRegistroActividad)
        {
            try
            {
                var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
                var desde = string.IsNullOrWhiteSpace(fechaDesde) ? hoy.AddDays(-6) : NegConversorFecha.ParseFechaCalendarioReporte(fechaDesde);
                var hasta = string.IsNullOrWhiteSpace(fechaHasta) ? hoy : NegConversorFecha.ParseFechaCalendarioReporte(fechaHasta);
                return ApiResults.Success(negRegistroActividad.ListarCierresAutomaticos(desde, hasta));
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

        /// <summary>
        /// Botón "Cerrar abrazos sin finalizar": cierra todos los abrazos en curso (de hoy o de días
        /// anteriores) y las asignaciones de días anteriores que nunca se iniciaron. Las asignaciones
        /// de hoy todavía sin iniciar quedan como están. Devuelve la cantidad cerrada.
        /// </summary>
        [HttpPost("resetearAbrazosColgados")]
        public IActionResult PostResetearAbrazosColgados()
        {
            try
            {
                var cantidad = negAsignacion.CerrarAbrazosSinFinalizar(incluirEnCursoDeHoy: true);
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
