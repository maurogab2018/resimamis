namespace ResimamisBackend.Entidades
{
    public class DetalleMovimiento
    {
        public int IdMovimiento { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = string.Empty;
        public int? IdBebe { get; set; }
        public string? NombreBebe { get; set; }
        public string? ApellidoBebe { get; set; }
        public int? IdVoluntaria { get; set; }
        public string? NombreVoluntaria { get; set; }
        public DateTime? FechaMovimiento { get; set; }
        public string? Observacion { get; set; }
        public int? Cantidad { get; set; }
        public string? EsEntrada { get; set; }
        public int? IdProveedor { get; set; }
        public string? NombreProveedor { get; set; }
        public string NombreMovimiento { get; set; } = string.Empty;
    }
}
