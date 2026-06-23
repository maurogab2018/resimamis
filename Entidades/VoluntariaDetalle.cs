namespace ResimamisBackend.Entidades
{
    public class VoluntariaDetalle
    {
        public int IdVoluntaria { get; set; }
        public int Dni { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public long Celular { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? IdEstado { get; set; }
        public int? IdRol { get; set; }
        public string? Rol { get; set; }
        public List<HorarioVoluntariaRespuesta> Horarios { get; set; } = new();
    }
}
