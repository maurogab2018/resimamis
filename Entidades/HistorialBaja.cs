namespace ResimamisBackend.Entidades
{
    /// <summary>Registro dado de baja (madre, bebé o voluntaria) para el historial de bajas.</summary>
    public class HistorialBajaItem
    {
        /// <summary>Madre, Bebé o Voluntaria.</summary>
        public string TipoRegistro { get; set; } = string.Empty;

        /// <summary>Id del registro; el detalle se consulta con el GET por id de cada recurso.</summary>
        public int IdRegistro { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public int? Dni { get; set; }

        /// <summary>
        /// Momento de la baja. Para bajas anteriores a la bitácora se usa la fecha de salida del
        /// bebé o la fecha fin de la voluntaria; null si no hay ningún dato.
        /// </summary>
        public DateTime? FechaBaja { get; set; }

        /// <summary>Quién registró la baja, si quedó en la bitácora.</summary>
        public string? Responsable { get; set; }

        /// <summary>Aclaración del motivo cuando se conoce (p. ej. egreso del NEO).</summary>
        public string? Motivo { get; set; }
    }

    /// <summary>Un abrazo cerrado por el sistema o por el botón de coordinación.</summary>
    public class CierreAutomaticoItem
    {
        public DateTime FechaHora { get; set; }

        public int IdAsignacion { get; set; }

        /// <summary>Bebé, voluntaria y desde cuándo estaba abierta.</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>"Cierre diario automático" o "Botón de coordinación (nombre)".</summary>
        public string Origen { get; set; } = string.Empty;
    }

    public class CierresAutomaticosRespuesta
    {
        public DateOnly FechaInicio { get; set; }

        public DateOnly FechaFin { get; set; }

        public int Total { get; set; }

        public List<CierreAutomaticoItem> Cierres { get; set; } = new();
    }

    public class HistorialBajasRespuesta
    {
        /// <summary>Filtro aplicado (Madre, Bebé, Voluntaria) o null si son todos.</summary>
        public string? TipoRegistro { get; set; }

        public int Total { get; set; }

        public List<HistorialBajaItem> Registros { get; set; } = new();
    }
}
