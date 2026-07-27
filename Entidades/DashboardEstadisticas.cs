namespace ResimamisBackend.Entidades
{
    public class EstadisticaAsignacionesPorDiaItem
    {
        public DateOnly Fecha { get; set; }
        public int CantidadAsignaciones { get; set; }
        public int CantidadAbrazos { get; set; }
    }

    public class EstadisticaAsignacionesPorDiaRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int TotalAsignaciones { get; set; }
        public int TotalAbrazos { get; set; }
        public List<EstadisticaAsignacionesPorDiaItem> PorDia { get; set; } = new();
    }

    public class EstadisticaDuracionAbrazosRespuesta
    {
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public int CantidadAbrazosFinalizados { get; set; }
        public double PromedioMinutos { get; set; }
        public double MinimoMinutos { get; set; }
        public double MaximoMinutos { get; set; }
        public double TotalMinutos { get; set; }
    }

    public class EstadisticaRangoEdadBebeItem
    {
        public string Rango { get; set; } = "";
        public int EdadMinDias { get; set; }
        public int? EdadMaxDias { get; set; }
        public int CantidadBebes { get; set; }
    }

    public class EstadisticaRangoEdadesBebesRespuesta
    {
        public int TotalBebes { get; set; }
        public List<EstadisticaRangoEdadBebeItem> Rangos { get; set; } = new();
    }

    public class EstadisticaPermanenciaBebeItem
    {
        public int IdBebe { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public DateTime? FechaIngresoNeo { get; set; }
        public int DiasPermanencia { get; set; }
        public string? EstadoBebe { get; set; }
        public string? NombreSala { get; set; }
    }

    public class EstadisticaPermanenciaBebesRespuesta
    {
        public int TotalBebes { get; set; }
        public double PromedioDias { get; set; }
        public int MinimoDias { get; set; }
        public int MaximoDias { get; set; }
        public List<EstadisticaPermanenciaBebeItem> Bebes { get; set; } = new();
    }

    public class EstadisticaVisitasPorDiaItem
    {
        public DateOnly Fecha { get; set; }
        public int Cantidad { get; set; }
    }

    public class EstadisticaVisitasPorFamiliarItem
    {
        public string Familiar { get; set; } = "";
        public int Cantidad { get; set; }
    }

    public class EstadisticaVisitasRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int TotalVisitas { get; set; }
        public int BebesVisitados { get; set; }
        public List<EstadisticaVisitasPorDiaItem> PorDia { get; set; } = new();
        public List<EstadisticaVisitasPorFamiliarItem> PorFamiliar { get; set; } = new();
    }

    public class DashboardResumenRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int AsignacionesEnPeriodo { get; set; }
        public int AbrazosFinalizadosEnPeriodo { get; set; }
        public int VisitasEnPeriodo { get; set; }
        public int BebesActivos { get; set; }
        public int BebesDisponiblesAbrazo { get; set; }
        public double PromedioDuracionAbrazoMinutos { get; set; }
        public double PromedioPermanenciaDias { get; set; }
    }

    public class AbrazosBebeDashboardRespuesta
    {
        public int IdBebe { get; set; }
        public string? NombreBebe { get; set; }
        public string? ApellidoBebe { get; set; }
        public DateOnly? FechaConsulta { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public int TotalAbrazos { get; set; }
        public int AbrazosFinalizados { get; set; }
        public List<AbrazoBebeDashboardItem> Abrazos { get; set; } = new();
    }

    public class AbrazoBebeDashboardItem
    {
        public int IdAsignacion { get; set; }
        public DateTime FechaHoraAsignacion { get; set; }
        public DateTime? FechaHoraInicio { get; set; }
        public DateTime? FechaHoraFin { get; set; }
        public double? DuracionMinutos { get; set; }
        public string EstadoAsignacion { get; set; } = "";
        public string NombreVoluntaria { get; set; } = "";
        public string? Comentario { get; set; }
    }

    public class AbrazosHoyResumen
    {
        public int Creados { get; set; }
        public int EnCurso { get; set; }
        public int Finalizados { get; set; }
    }

    public class DashboardCoordinacionHoyRespuesta
    {
        public DateOnly Fecha { get; set; }
        public int BebesActivos { get; set; }
        public int BebesDisponiblesAbrazo { get; set; }
        public int BebesAsignados { get; set; }
        public AbrazosHoyResumen AbrazosHoy { get; set; } = new();
        public int VoluntariasConAsistenciaHoy { get; set; }
        public int AbrazosColgados { get; set; }
        public int VisitasHoy { get; set; }
    }

    public class BebeSinAbrazoHoyItem
    {
        public int IdBebe { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? EstadoBebe { get; set; }
        public string? NombreSala { get; set; }
    }

    public class DashboardCoberturaHoyRespuesta
    {
        public DateOnly Fecha { get; set; }
        public int TotalBebesActivos { get; set; }
        public int BebesConAbrazoFinalizadoHoy { get; set; }
        public double PorcentajeCobertura { get; set; }
        public List<BebeSinAbrazoHoyItem> BebesSinAbrazoHoy { get; set; } = new();
    }

    public class EstadisticaBebesPorEstadoItem
    {
        public string EstadoBebe { get; set; } = "";
        public int Cantidad { get; set; }
    }

    public class EstadisticaBebesPorEstadoRespuesta
    {
        public int TotalBebes { get; set; }
        public List<EstadisticaBebesPorEstadoItem> PorEstado { get; set; } = new();
    }

    public class EstadisticaBebesPorSalaItem
    {
        public int? IdSala { get; set; }
        public string NombreSala { get; set; } = "";
        public int CantidadBebes { get; set; }
        public double PromedioPermanenciaDias { get; set; }
    }

    public class EstadisticaBebesPorSalaRespuesta
    {
        public int TotalBebes { get; set; }
        public List<EstadisticaBebesPorSalaItem> PorSala { get; set; } = new();
    }

    public class RankingVoluntariaAbrazosItem
    {
        public int Posicion { get; set; }
        public int IdVoluntaria { get; set; }
        public string NombreVoluntaria { get; set; } = "";
        public int CantidadAbrazosFinalizados { get; set; }
    }

    public class RankingVoluntariasAbrazosRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int Top { get; set; }
        public List<RankingVoluntariaAbrazosItem> Ranking { get; set; } = new();
    }

    public class EvolucionPesoBebeItem
    {
        public int IdBebe { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? NombreSala { get; set; }
        public DateTime? FechaIngresoNeo { get; set; }
        public DateTime? FechaSalida { get; set; }
        public decimal? PesoNacimiento { get; set; }
        public decimal? PesoIngresoNeo { get; set; }
        public decimal? PesoDiaAbrazos { get; set; }
        public decimal? PesoEgreso { get; set; }
        /// <summary>Peso egreso (PesoAlta) − peso ingreso NEO. Null si falta alguno.</summary>
        public decimal? DiferenciaIngresoEgreso { get; set; }
        /// <summary>Variación % respecto al peso de ingreso. Null si no hay base.</summary>
        public double? PorcentajeVariacion { get; set; }
        public bool TieneComparacionCompleta { get; set; }
    }

    public class EvolucionPesoBebesRespuesta
    {
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public int TotalBebes { get; set; }
        public int BebesConComparacionCompleta { get; set; }
        public int BebesConGanancia { get; set; }
        public int BebesConPerdida { get; set; }
        public int BebesSinCambio { get; set; }
        public decimal? PromedioPesoIngreso { get; set; }
        public decimal? PromedioPesoEgreso { get; set; }
        /// <summary>Ganancia de peso promedio en gramos (egreso − ingreso).</summary>
        public decimal? PromedioDiferencia { get; set; }
        /// <summary>Alias de <see cref="PromedioDiferencia"/> (gramos).</summary>
        public decimal? PromedioGanancia => PromedioDiferencia;
        /// <summary>Ganancia mínima en gramos (puede ser negativa si hubo pérdida).</summary>
        public decimal? GananciaMinima { get; set; }
        /// <summary>Ganancia máxima en gramos.</summary>
        public decimal? GananciaMaxima { get; set; }
        public List<EvolucionPesoBebeItem> Bebes { get; set; } = new();
    }
}
