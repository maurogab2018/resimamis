namespace ResimamisBackend.Entidades
{
    /// <summary>Ítem del reporte de asistencias por período.</summary>
    public class ReporteAsistenciaPeriodoItem
    {
        public int IdAsistencia { get; set; }

        public int IdVoluntaria { get; set; }

        public string NombreVoluntaria { get; set; } = "";

        public string ApellidoVoluntaria { get; set; } = "";

        /// <summary>Ingreso en UTC (timestamptz); el cliente puede convertir a hora local.</summary>
        public DateTime? FechaHoraIngreso { get; set; }

        public DateTime? FechaHoraSalida { get; set; }

        /// <summary>Duración en sitio si hay ingreso y salida registrados.</summary>
        public double? DuracionMinutos { get; set; }

        public string EstadoAsistencia { get; set; } = "";
    }

    /// <summary>Respuesta del reporte por rango de fechas.</summary>
    public class ReporteAsistenciaPeriodoRespuesta
    {
        /// <summary>Fecha inicio del filtro (solo fecha, inclusive).</summary>
        public DateOnly FechaInicio { get; set; }

        /// <summary>Fecha fin del filtro (solo fecha, inclusive).</summary>
        public DateOnly FechaFin { get; set; }

        public int TotalRegistros { get; set; }

        public List<ReporteAsistenciaPeriodoItem> Registros { get; set; } = new();
    }
}
