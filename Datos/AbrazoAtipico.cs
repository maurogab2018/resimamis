namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Texto con el que el reset de días anteriores avisa en el comentario que cerró el
    /// abrazo. Es solo para el usuario: quién entra o no en las estadísticas de duración
    /// lo decide la columna <c>ASIGNACION.cierreAutomatico</c>, no este texto.
    /// </summary>
    public static class AbrazoAtipico
    {
        public const string MarcaCierreAutomatico = "Cierre automático";
    }
}
