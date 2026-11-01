namespace ResimamisBackend.Entidades
{
    /// <summary>Un día en que una voluntaria asistió o tuvo asignaciones, con su turno.</summary>
    public class DiaTurnoVoluntariaItem
    {
        public int IdVoluntaria { get; set; }

        public string Nombre { get; set; } = "";

        public string Apellido { get; set; } = "";

        /// <summary>Día calendario en Argentina.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Nombre del día (Lunes, Martes, ...).</summary>
        public string Dia { get; set; } = "";

        /// <summary>
        /// Turno asignado a la voluntaria para ese día de la semana; si no tiene horario cargado se
        /// infiere por la hora de ingreso. "Sin turno asignado" si no hay forma de saberlo.
        /// </summary>
        public string Turno { get; set; } = "";

        /// <summary>De dónde salió el turno: "Horario de la voluntaria", "Según hora de ingreso" o "Sin datos".</summary>
        public string OrigenTurno { get; set; } = "";

        /// <summary>Primer ingreso del día (UTC), si registró asistencia.</summary>
        public DateTime? FechaHoraIngreso { get; set; }

        /// <summary>Última salida del día (UTC), si la registró.</summary>
        public DateTime? FechaHoraSalida { get; set; }

        /// <summary>true si registró asistencia ese día.</summary>
        public bool TieneAsistencia { get; set; }

        /// <summary>Asignaciones (abrazos o tareas) que tuvo ese día.</summary>
        public int CantidadAsignaciones { get; set; }
    }

    public class DiasTurnosVoluntariasRespuesta
    {
        public DateOnly FechaInicio { get; set; }

        public DateOnly FechaFin { get; set; }

        /// <summary>Voluntaria filtrada, o null si son todas.</summary>
        public int? IdVoluntaria { get; set; }

        public int TotalRegistros { get; set; }

        public List<DiaTurnoVoluntariaItem> Registros { get; set; } = new();
    }
}
