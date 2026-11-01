namespace ResimamisBackend.Entidades
{
    /// <summary>Un movimiento del día dentro del reporte diario.</summary>
    public class ReporteDiarioMovimiento
    {
        /// <summary>Momento del movimiento (UTC).</summary>
        public DateTime FechaHora { get; set; }

        /// <summary>Abrazos, Movimientos de insumos, Altas, Bajas, Asignaciones o Asistencias.</summary>
        public string Categoria { get; set; } = "";

        /// <summary>Subtipo dentro de la categoría (p. ej. Entrada/Salida, Madre/Bebé, Abrazo/Tarea).</summary>
        public string Tipo { get; set; } = "";

        /// <summary>Texto listo para mostrar con la información disponible del movimiento.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>Quién lo realizó o registró, si se conoce.</summary>
        public string? Responsable { get; set; }

        /// <summary>Fin del movimiento cuando tiene duración (abrazos, asistencias).</summary>
        public DateTime? FechaHoraFin { get; set; }
    }

    public class ReporteDiarioCategoria
    {
        public string Categoria { get; set; } = "";

        public int Cantidad { get; set; }

        /// <summary>Del más antiguo al más reciente.</summary>
        public List<ReporteDiarioMovimiento> Movimientos { get; set; } = new();
    }

    public class ReporteDiarioRespuesta
    {
        public DateOnly Fecha { get; set; }

        public int TotalMovimientos { get; set; }

        /// <summary>Siempre las seis categorías, en orden fijo, aunque alguna venga vacía.</summary>
        public List<ReporteDiarioCategoria> Categorias { get; set; } = new();
    }

    public static class CategoriasReporteDiario
    {
        public const string Abrazos = "Abrazos";
        public const string MovimientosInsumos = "Movimientos de insumos";
        public const string Altas = "Altas";
        public const string Bajas = "Bajas";
        public const string Asignaciones = "Asignaciones";
        public const string Asistencias = "Asistencias";

        public static readonly string[] Orden =
            { Abrazos, MovimientosInsumos, Altas, Bajas, Asignaciones, Asistencias };
    }
}
