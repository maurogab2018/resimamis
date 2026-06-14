namespace ResimamisBackend.Entidades
{
    public class UsuarioListado
    {
        public int IdUsuario { get; set; }
        public int Dni { get; set; }
        public int IdVoluntaria { get; set; }
        public string NombreVoluntaria { get; set; } = string.Empty;
        public string ApellidoVoluntaria { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public int? IdEstado { get; set; }
        public string? NombreEstado { get; set; }
    }
}
