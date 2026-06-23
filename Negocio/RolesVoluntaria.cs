namespace ResimamisBackend.Negocio
{
    public static class RolesVoluntaria
    {
        public const int IdVoluntaria = 2;
        public const int IdCoordinadora = 3;

        private static readonly string[] NombresCoordinadora =
        {
            "Coordinadora",
            "Administrativa",
            "Administrador",
            "Admin"
        };

        /// <summary>Rol con permisos de gestión (antes Administrativa / Admin).</summary>
        public static bool EsCoordinadora(int? idRol, string? nombreRol)
        {
            if (idRol == IdCoordinadora)
                return true;

            return !string.IsNullOrWhiteSpace(nombreRol)
                   && NombresCoordinadora.Any(n =>
                       string.Equals(nombreRol, n, StringComparison.OrdinalIgnoreCase));
        }

        public static bool EsRolPermitido(int? idRol) =>
            !idRol.HasValue || idRol.Value is IdVoluntaria or IdCoordinadora;
    }
}
