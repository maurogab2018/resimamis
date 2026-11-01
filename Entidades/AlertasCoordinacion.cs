namespace ResimamisBackend.Entidades
{
    /// <summary>Bebé activo con los días que lleva sin un abrazo finalizado.</summary>
    public class BebeSinAbrazoItem
    {
        public int IdBebe { get; set; }
        public string Nombre { get; set; } = "";
        public string? Sala { get; set; }
        /// <summary>Fecha (UTC) del último abrazo finalizado; null si nunca recibió uno.</summary>
        public DateTime? UltimoAbrazo { get; set; }
        /// <summary>Días desde el último abrazo, o desde el ingreso a NEO si nunca recibió uno.</summary>
        public int? DiasSinAbrazo { get; set; }
        public bool NuncaAbrazado => UltimoAbrazo == null;
    }

    /// <summary>Voluntaria activa con los días desde su última asistencia.</summary>
    public class VoluntariaSinVenirItem
    {
        public int IdVoluntaria { get; set; }
        public string Nombre { get; set; } = "";
        /// <summary>Último ingreso (UTC); null si nunca registró asistencia.</summary>
        public DateTime? UltimaAsistencia { get; set; }
        public int? DiasSinVenir { get; set; }
    }

    public class AlertasCoordinacionRespuesta
    {
        /// <summary>Bebés activos ordenados por más días sin abrazo; NuncaAbrazado marca a los que no recibieron ninguno.</summary>
        public List<BebeSinAbrazoItem> BebesMasDiasSinAbrazo { get; set; } = new();
        /// <summary>Voluntarias activas que no vienen hace al menos <c>DiasUmbralVoluntarias</c> días.</summary>
        public List<VoluntariaSinVenirItem> VoluntariasSinVenir { get; set; } = new();
        public int DiasUmbralVoluntarias { get; set; }
    }
}
