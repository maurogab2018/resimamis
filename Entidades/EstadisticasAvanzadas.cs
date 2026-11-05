namespace ResimamisBackend.Entidades
{
    // Respuestas de api/Estadisticas/*. Todas las fechas-hora van en UTC (como el resto de la API)
    // y las fechas calendario en hora Argentina. "Abrazo real" = de bebé, iniciado, finalizado y
    // no cerrado automáticamente.

    // ---------- 1. Impacto del programa ----------
    public class ImpactoTramoItem
    {
        /// <summary>Ej. "0-5 abrazos".</summary>
        public string Tramo { get; set; } = "";
        public int CantidadBebes { get; set; }
        public double PromedioAbrazos { get; set; }
        /// <summary>Ganancia de peso diaria promedio entre ingreso a NEO y egreso; null si ningún bebé del tramo tiene ambos pesos.</summary>
        public double? PromedioGananciaGramosPorDia { get; set; }
        public double PromedioDiasInternacion { get; set; }
    }

    public class ImpactoBebeItem
    {
        public int IdBebe { get; set; }
        public string Nombre { get; set; } = "";
        public int Abrazos { get; set; }
        public int DiasInternacion { get; set; }
        public double? GananciaGramosPorDia { get; set; }
    }

    public class ImpactoProgramaRespuesta
    {
        public string Aclaracion { get; set; } = "";
        public int BebesEgresados { get; set; }
        public List<ImpactoTramoItem> Tramos { get; set; } = new();
        public List<ImpactoBebeItem> Bebes { get; set; } = new();
    }

    // ---------- 2. Días hasta el primer abrazo ----------
    public class PrimerAbrazoBebeItem
    {
        public int IdBebe { get; set; }
        public string Nombre { get; set; } = "";
        public bool Activo { get; set; }
        public DateOnly? FechaIngresoNeo { get; set; }
        public DateOnly? FechaPrimerAbrazo { get; set; }
        /// <summary>Días entre ingreso y primer abrazo; si todavía no lo recibió, días que lleva esperando.</summary>
        public int? Dias { get; set; }
        public bool SinPrimerAbrazo { get; set; }
    }

    public class PrimerAbrazoMesItem
    {
        /// <summary>"yyyy-MM" del ingreso a NEO.</summary>
        public string MesIngreso { get; set; } = "";
        public int Bebes { get; set; }
        public double? PromedioDias { get; set; }
    }

    public class PrimerAbrazoRespuesta
    {
        public double? PromedioDias { get; set; }
        public double? MedianaDias { get; set; }
        public int BebesConPrimerAbrazo { get; set; }
        /// <summary>Bebés activos que todavía no recibieron ningún abrazo.</summary>
        public int ActivosSinPrimerAbrazo { get; set; }
        public List<PrimerAbrazoMesItem> PorMesDeIngreso { get; set; } = new();
        public List<PrimerAbrazoBebeItem> Bebes { get; set; } = new();
    }

    // ---------- 3. Equidad entre bebés ----------
    public class EquidadBebeItem
    {
        public int IdBebe { get; set; }
        public string Nombre { get; set; } = "";
        public string? Sala { get; set; }
        public int DiasInternado { get; set; }
        public int Abrazos { get; set; }
        public double AbrazosPorSemana { get; set; }
        /// <summary>Recibe menos de la mitad del promedio de abrazos por semana.</summary>
        public bool PorDebajoDelPromedio { get; set; }
    }

    public class EquidadBebesRespuesta
    {
        public double PromedioAbrazosPorSemana { get; set; }
        public int BebesPorDebajo { get; set; }
        /// <summary>De menos a más abrazos por semana.</summary>
        public List<EquidadBebeItem> Bebes { get; set; } = new();
    }

    // ---------- 4. Cumplimiento del horario ----------
    public class CumplimientoVoluntariaItem
    {
        public int IdVoluntaria { get; set; }
        public string Nombre { get; set; } = "";
        /// <summary>Ej. "Lunes, Miércoles".</summary>
        public string DiasDelHorario { get; set; } = "";
        public int DiasPrevistos { get; set; }
        public int DiasQueVino { get; set; }
        /// <summary>Vino en un día que no estaba en su horario.</summary>
        public int DiasExtra { get; set; }
        public double PorcentajeCumplimiento { get; set; }
    }

    public class CumplimientoHorarioRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public double? PromedioCumplimiento { get; set; }
        /// <summary>De menor a mayor cumplimiento.</summary>
        public List<CumplimientoVoluntariaItem> Voluntarias { get; set; } = new();
    }

    // ---------- 5. Proyección de insumos ----------
    public static class EstadosProyeccionInsumo
    {
        public const string BajoMinimo = "Bajo mínimo";
        public const string Critico = "Crítico";
        public const string Atencion = "Atención";
        public const string Ok = "OK";
        public const string SinConsumo = "Sin consumo";
    }

    public class ProyeccionInsumoItem
    {
        public int IdInsumo { get; set; }
        public string Nombre { get; set; } = "";
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public double ConsumoDiarioPromedio { get; set; }
        /// <summary>Días hasta llegar al mínimo al ritmo actual; null si no hubo consumo.</summary>
        public double? DiasHastaMinimo { get; set; }
        public double? DiasHastaAgotarse { get; set; }
        /// <summary>Bajo mínimo, Crítico (≤7 días), Atención (≤15 días), OK o Sin consumo.</summary>
        public string Estado { get; set; } = "";
    }

    public class ProyeccionInsumosRespuesta
    {
        public int DiasAnalizados { get; set; }
        /// <summary>Los más urgentes primero.</summary>
        public List<ProyeccionInsumoItem> Insumos { get; set; } = new();
    }

    // ---------- 6. Mapa de calor ----------
    public class MapaCalorCelda
    {
        /// <summary>1 = Lunes ... 7 = Domingo.</summary>
        public int IdDia { get; set; }
        public string Dia { get; set; } = "";
        /// <summary>0 a 23, hora Argentina.</summary>
        public int Hora { get; set; }
        public int Abrazos { get; set; }
        public int Asistencias { get; set; }
    }

    public class MapaCalorRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int TotalAbrazos { get; set; }
        public int TotalAsistencias { get; set; }
        /// <summary>Solo las celdas con actividad.</summary>
        public List<MapaCalorCelda> Celdas { get; set; } = new();
    }

    // ---------- 7. Tasa de abrazos completados ----------
    public class TasaCompletadosGrupoItem
    {
        /// <summary>Nombre de la voluntaria o del día de la semana.</summary>
        public string Grupo { get; set; } = "";
        public int? IdVoluntaria { get; set; }
        public int Total { get; set; }
        public int Completados { get; set; }
        public int CerradosAutomaticamente { get; set; }
        public int NuncaIniciados { get; set; }
        public double PorcentajeCompletados { get; set; }
    }

    public class TasaCompletadosRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int Total { get; set; }
        public int Completados { get; set; }
        /// <summary>Cerrados por el sistema o el botón después de iniciarse.</summary>
        public int CerradosAutomaticamente { get; set; }
        /// <summary>Asignados y nunca iniciados.</summary>
        public int NuncaIniciados { get; set; }
        public int EnCurso { get; set; }
        public double PorcentajeCompletados { get; set; }
        public List<TasaCompletadosGrupoItem> PorVoluntaria { get; set; } = new();
        public List<TasaCompletadosGrupoItem> PorDiaSemana { get; set; } = new();
    }

    // ---------- 8. Bebés sin visitas ----------
    public class BebeSinVisitasItem
    {
        public int IdBebe { get; set; }
        public string Nombre { get; set; } = "";
        public string? Sala { get; set; }
        public DateTime? UltimaVisita { get; set; }
        /// <summary>Días desde la última visita o, si nunca tuvo, desde el ingreso a NEO.</summary>
        public int? DiasSinVisita { get; set; }
        public bool NuncaVisitado { get; set; }
    }

    public class BebesSinVisitasRespuesta
    {
        public int UmbralDias { get; set; }
        public int BebesSuperanUmbral { get; set; }
        /// <summary>Solo los que superan el umbral, de más a menos días.</summary>
        public List<BebeSinVisitasItem> Bebes { get; set; } = new();
    }

    // ---------- 9. Rendimiento de voluntarias ----------
    public class RendimientoVoluntariaItem
    {
        public int IdVoluntaria { get; set; }
        public string Nombre { get; set; } = "";
        public double HorasEnHospital { get; set; }
        public int Abrazos { get; set; }
        public double MinutosAbrazando { get; set; }
        public double? AbrazosPorHora { get; set; }
        /// <summary>Minutos abrazando / minutos en el hospital.</summary>
        public double? PorcentajeTiempoAbrazando { get; set; }
    }

    public class RendimientoVoluntariasRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public string Aclaracion { get; set; } = "";
        /// <summary>De más a menos abrazos por hora.</summary>
        public List<RendimientoVoluntariaItem> Voluntarias { get; set; } = new();
    }

    // ---------- 10. Duración por sala y por peso ----------
    public class DuracionGrupoItem
    {
        public string Grupo { get; set; } = "";
        public int Abrazos { get; set; }
        public double PromedioMinutos { get; set; }
        public double MedianaMinutos { get; set; }
    }

    public class DuracionPorGrupoRespuesta
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public List<DuracionGrupoItem> PorSala { get; set; } = new();
        /// <summary>Por peso del bebé (peso del día de abrazos o, si no hay, de ingreso a NEO).</summary>
        public List<DuracionGrupoItem> PorPeso { get; set; } = new();
    }
}
