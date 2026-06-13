using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public static class ValidacionTextoPersona
    {
        /// <summary>Letras Unicode (ñ, tildes) y espacios. Sin dígitos ni símbolos.</summary>
        private static readonly Regex NombreApellidoPattern =
            new(@"^[\p{L} ]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool EsNombreApellidoValido(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) && NombreApellidoPattern.IsMatch(valor.Trim());

        public static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? valor : valor.Trim();
    }
}
