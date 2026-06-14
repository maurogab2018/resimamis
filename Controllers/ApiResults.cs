using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Controllers
{
    public static class ApiResults
    {
        /// <summary>Respuesta exitosa. HTTP 200.</summary>
        public static IActionResult Success(object? data, string? message = null) =>
            new OkObjectResult(Build(true, data, message, Array.Empty<string>()));

        /// <summary>Error de negocio/validación. HTTP 200 con success=false.</summary>
        public static IActionResult BadRequest(string message, IEnumerable<string>? errors = null) =>
            new OkObjectResult(Build(false, null, message, NormalizeErrors(message, errors)));

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
