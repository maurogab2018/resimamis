namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Abrazo cerrado por el reset de días anteriores. La duración no es real
    /// (el fin se graba al momento del cierre) y no entra en estadísticas de duración.
    /// </summary>
    public static class AbrazoAtipico
    {
        public const string MarcaCierreAutomatico = "Cierre automático";
    }
}
