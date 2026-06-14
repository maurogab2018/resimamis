using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Controllers
{
    public static class ApiResults
    {
        /// <summary>Respuesta exitosa. HTTP 200.</summary>
        public static IActionResult Success(object? data, string? message = null) =>
            new OkObjectResult(Build(true, data, message, Array.Empty<string>()));

        /// <summary>Recurso creado. HTTP 201.</summary>
        public static IActionResult Created(object? data, string? message = null) =>
            new ObjectResult(Build(true, data, message, Array.Empty<string>())) { StatusCode = 201 };

        /// <summary>Error de negocio/validación del cliente. HTTP 400.</summary>
        public static IActionResult BadRequest(string message, IEnumerable<string>? errors = null) =>
            new BadRequestObjectResult(Build(false, null, message, NormalizeErrors(message, errors)));

        /// <summary>Recurso no encontrado. HTTP 404.</summary>
        public static IActionResult NotFound(string message) =>
            new NotFoundObjectResult(Build(false, null, message, NormalizeErrors(message, null)));

        /// <summary>No autenticado. HTTP 401.</summary>
        public static IActionResult Unauthorized(string message = "No autenticado.") =>
            new UnauthorizedObjectResult(Build(false, null, message, NormalizeErrors(message, null)));

        /// <summary>Error interno del servidor. HTTP 500.</summary>
        public static IActionResult InternalServerError(string message = "Error interno del servidor.") =>
            new ObjectResult(Build(false, null, message, NormalizeErrors(message, null))) { StatusCode = 500 };

        public static IActionResult ValidationError(IEnumerable<string> errors)
        {
            var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            var message = list.FirstOrDefault() ?? "Error de validación";
            return BadRequest(message, list);
        }

        private static ApiResponse Build(bool success, object? data, string? message, IEnumerable<string> errors) =>
            new()
            {
                success = success,
                data = data,
                message = message,
                errors = errors.ToList()
            };

        private static IEnumerable<string> NormalizeErrors(string? message, IEnumerable<string>? errors)
        {
            var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            if (!string.IsNullOrWhiteSpace(message) && !list.Contains(message))
                list.Insert(0, message);
            if (list.Count == 0 && !string.IsNullOrWhiteSpace(message))
                list.Add(message);
            return list;
        }
    }
}
