namespace ResimamisBackend.Entidades
{
    public class VisitaListado
    {
        public int idVisita { get; set; }
        public int idBebe { get; set; }
        public string? nombreBebe { get; set; }
        public string? apellidoBebe { get; set; }
        public string nombreVisitante { get; set; } = string.Empty;
        public string familiar { get; set; } = string.Empty;
        public DateTime fechaHoraVisita { get; set; }
        public string? observacion { get; set; }
        public int? documentoVisitante { get; set; }
        public long? telefonoVisitante { get; set; }
        public bool activa { get; set; }
        public DateTime fechaRegistro { get; set; }
    }
}
