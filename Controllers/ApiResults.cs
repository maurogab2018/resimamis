using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Controllers
{
    public static class ApiResults
    {
        /// <summary>Respuesta exitosa. HTTP 200.</summary>
        public static IActionResult Success(object? data) =>
            new OkObjectResult(new { data });

        /// <summary>Recurso creado. HTTP 201.</summary>
        public static IActionResult Created(object? data) =>
            new ObjectResult(new { data }) { StatusCode = 201 };

        /// <summary>Error de negocio/validación del cliente. HTTP 400.</summary>
        public static IActionResult BadRequest(string message, IEnumerable<string>? errors = null) =>
            new BadRequestObjectResult(BuildError(message, errors));

        /// <summary>Recurso no encontrado. HTTP 404.</summary>
        public static IActionResult NotFound(string message) =>
            new NotFoundObjectResult(BuildError(message, null));

        /// <summary>No autenticado. HTTP 401.</summary>
        public static IActionResult Unauthorized(string message = "No autenticado.") =>
            new UnauthorizedObjectResult(BuildError(message, null));

        /// <summary>Sin permisos. HTTP 403.</summary>
        public static IActionResult Forbidden(string message = "No autorizado.") =>
            new ObjectResult(BuildError(message, null)) { StatusCode = 403 };

        /// <summary>Conflicto de estado o duplicado. HTTP 409.</summary>
        public static IActionResult Conflict(string message) =>
            new ObjectResult(BuildError(message, null)) { StatusCode = 409 };

        /// <summary>Error interno del servidor. HTTP 500.</summary>
        public static IActionResult InternalServerError(string message = "Error interno del servidor.") =>
            new ObjectResult(BuildError(message, null)) { StatusCode = 500 };

        public static IActionResult ValidationError(IEnumerable<string> errors)
        {
            var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            var message = list.FirstOrDefault() ?? "Error de validación";
            return BadRequest(message, list);
        }

        private static ApiResponse BuildError(string message, IEnumerable<string>? errors)
        {
            var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (list == null || list.Count == 0)
                list = new List<string> { message };
            return new ApiResponse { message = message, errors = list };
        }
    }
}
