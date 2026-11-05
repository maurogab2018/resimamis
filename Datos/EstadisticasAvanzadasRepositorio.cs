using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Estadísticas de análisis, de solo lectura. Se calculan en memoria sobre consultas acotadas:
    /// el volumen del programa (decenas de bebés, cientos de abrazos) lo permite y deja las reglas
    /// a la vista. "Abrazo real" = de bebé, iniciado, finalizado y no cerrado automáticamente.
    /// </summary>
    public class EstadisticasAvanzadasRepositorio : IEstadisticasAvanzadasRepositorio
    {
        private static readonly System.Globalization.CultureInfo CulturaAr = new("es-AR");

        // Una asistencia sin salida o más larga que esto no se cuenta para horas en el hospital.
        private static readonly TimeSpan JornadaMaxima = TimeSpan.FromHours(12);

        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;

        public EstadisticasAvanzadasRepositorio(ApplicationDbContext db, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
        }

        // ---------- helpers ----------

        private static DateOnly Hoy() => NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);

        private static DateOnly Dia(DateTime utc) => NegConversorFecha.FechaCalendarioArgentina(utc);

        /// <summary>Id de la tabla DIA: 1 = Lunes ... 7 = Domingo.</summary>
        private static int IdDia(DayOfWeek d) => d == DayOfWeek.Sunday ? 7 : (int)d;

        private static string NombreDia(int idDia) =>
            CulturaAr.TextInfo.ToTitleCase(CulturaAr.DateTimeFormat.GetDayName((DayOfWeek)(idDia % 7)));

        /// <summary>Los pesos están cargados a veces en kg (1.85) y a veces en gramos (2550).</summary>
        private static double? PesoEnGramos(decimal? peso) =>
            peso is null or <= 0 ? null : (double)(peso.Value < 20 ? peso.Value * 1000 : peso.Value);

        private static double Mediana(List<double> valores)
        {
            if (valores.Count == 0)
                return 0;
            var o = valores.OrderBy(v => v).ToList();
            return o.Count % 2 == 1 ? o[o.Count / 2] : (o[o.Count / 2 - 1] + o[o.Count / 2]) / 2;
        }

        private static double R(double v, int dec = 1) => Math.Round(v, dec);

        private int IdElimAsignaciones() => estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

        private sealed record AbrazoReal(int IdBebe, int IdVoluntaria, DateTime Inicio, DateTime Fin, double Minutos);

        /// <summary>Abrazos reales, opcionalmente con inicio dentro del rango UTC.</summary>
        private List<AbrazoReal> AbrazosReales(DateTime? inicioUtc = null, DateTime? finUtc = null)
        {
            var idElim = IdElimAsignaciones();
            var q = db.ASIGNACION.AsNoTracking()
                .Where(a => a.idBebe != null && a.fechaHoraInicio != null && a.fechaHoraFin != null
                            && !a.cierreAutomatico && a.idEstado != idElim);
            if (inicioUtc.HasValue)
                q = q.Where(a => a.fechaHoraInicio >= inicioUtc && a.fechaHoraInicio < finUtc);
            return q.Select(a => new { IdBebe = a.idBebe!.Value, a.idVoluntaria, Inicio = a.fechaHoraInicio!.Value, Fin = a.fechaHoraFin!.Value })
                .ToList()
                .Select(a => new AbrazoReal(a.IdBebe, a.idVoluntaria, a.Inicio, a.Fin, (a.Fin - a.Inicio).TotalMinutes))
                .Where(a => a.Minutos > 0)
                .ToList();
        }

        private List<BEBE> BebesActivos()
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Bebes");
            return db.BEBE.AsNoTracking().Include(b => b.Sala)
                .Where(b => b.FechaSalida == null && b.IdEstado != idElim)
                .ToList();
        }

        private static string NombreBebe(BEBE b) => $"{b.nombre} {b.apellido}".Trim();

        // ---------- 1. Impacto del programa ----------

        public ImpactoProgramaRespuesta ObtenerImpactoPrograma(DateTime? egresoDesde, DateTime? egresoHasta)
        {
            var egresados = db.BEBE.AsNoTracking()
                .Where(b => b.FechaSalida != null && b.FechaIngresoNEO != null && b.FechaSalida >= b.FechaIngresoNEO)
                .ToList()
                .Where(b => egresoDesde == null || DateOnly.FromDateTime(b.FechaSalida!.Value) >= DateOnly.FromDateTime(egresoDesde.Value))
                .Where(b => egresoHasta == null || DateOnly.FromDateTime(b.FechaSalida!.Value) <= DateOnly.FromDateTime(egresoHasta.Value))
                .ToList();

            var abrazosPorBebe = AbrazosReales().GroupBy(a => a.IdBebe).ToDictionary(g => g.Key, g => g.Count());

            var bebes = egresados.Select(b =>
            {
                var dias = Math.Max(1, (b.FechaSalida!.Value.Date - b.FechaIngresoNEO!.Value.Date).Days);
                var ingreso = PesoEnGramos(b.PesoIngresoNEO);
                var alta = PesoEnGramos(b.PesoAlta);
                return new ImpactoBebeItem
                {
                    IdBebe = b.ID,
                    Nombre = NombreBebe(b),
                    Abrazos = abrazosPorBebe.GetValueOrDefault(b.ID),
                    DiasInternacion = dias,
                    GananciaGramosPorDia = ingreso.HasValue && alta.HasValue ? R((alta.Value - ingreso.Value) / dias) : null
                };
            }).OrderByDescending(b => b.Abrazos).ToList();

            (string Nombre, int Min, int Max)[] tramos = { ("0-5 abrazos", 0, 5), ("6-15 abrazos", 6, 15), ("16 o más abrazos", 16, int.MaxValue) };
            return new ImpactoProgramaRespuesta
            {
                Aclaracion = "Bebés ya egresados, agrupados por abrazos recibidos durante la internación. Muestra una relación, no prueba que los abrazos causen la diferencia.",
                BebesEgresados = bebes.Count,
                Bebes = bebes,
                Tramos = tramos.Select(t =>
                {
                    var grupo = bebes.Where(b => b.Abrazos >= t.Min && b.Abrazos <= t.Max).ToList();
                    var conPeso = grupo.Where(b => b.GananciaGramosPorDia.HasValue).ToList();
                    return new ImpactoTramoItem
                    {
                        Tramo = t.Nombre,
                        CantidadBebes = grupo.Count,
                        PromedioAbrazos = grupo.Count == 0 ? 0 : R(grupo.Average(b => b.Abrazos)),
                        PromedioGananciaGramosPorDia = conPeso.Count == 0 ? null : R(conPeso.Average(b => b.GananciaGramosPorDia!.Value)),
                        PromedioDiasInternacion = grupo.Count == 0 ? 0 : R(grupo.Average(b => b.DiasInternacion))
                    };
                }).ToList()
            };
        }

        // ---------- 2. Días hasta el primer abrazo ----------

        public PrimerAbrazoRespuesta ObtenerPrimerAbrazo()
        {
            var hoy = Hoy();
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Bebes");
            var bebes = db.BEBE.AsNoTracking().Where(b => b.FechaIngresoNEO != null).ToList();
            var primero = AbrazosReales().GroupBy(a => a.IdBebe).ToDictionary(g => g.Key, g => g.Min(a => a.Inicio));

            var items = bebes.Select(b =>
            {
                var ingreso = DateOnly.FromDateTime(b.FechaIngresoNEO!.Value);
                var activo = b.FechaSalida == null && b.IdEstado != idElim;
                DateOnly? fechaPrimero = primero.TryGetValue(b.ID, out var p) ? Dia(p) : null;
                int? dias = fechaPrimero.HasValue
                    ? Math.Max(0, fechaPrimero.Value.DayNumber - ingreso.DayNumber)
                    : activo ? Math.Max(0, hoy.DayNumber - ingreso.DayNumber) : null;
                return new PrimerAbrazoBebeItem
                {
                    IdBebe = b.ID,
                    Nombre = NombreBebe(b),
                    Activo = activo,
                    FechaIngresoNeo = ingreso,
                    FechaPrimerAbrazo = fechaPrimero,
                    Dias = dias,
                    SinPrimerAbrazo = fechaPrimero == null
                };
            })
            // Los egresados sin ningún abrazo no aportan a "días hasta el primero".
            .Where(i => i.FechaPrimerAbrazo.HasValue || i.Activo)
            .OrderByDescending(i => i.SinPrimerAbrazo).ThenByDescending(i => i.Dias)
            .ToList();

            var con = items.Where(i => i.FechaPrimerAbrazo.HasValue).Select(i => (double)i.Dias!.Value).ToList();
            return new PrimerAbrazoRespuesta
            {
                PromedioDias = con.Count == 0 ? null : R(con.Average()),
                MedianaDias = con.Count == 0 ? null : Mediana(con),
                BebesConPrimerAbrazo = con.Count,
                ActivosSinPrimerAbrazo = items.Count(i => i.Activo && i.SinPrimerAbrazo),
                Bebes = items,
                PorMesDeIngreso = items.Where(i => i.FechaPrimerAbrazo.HasValue)
                    .GroupBy(i => i.FechaIngresoNeo!.Value.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .Select(g => new PrimerAbrazoMesItem { MesIngreso = g.Key, Bebes = g.Count(), PromedioDias = R(g.Average(i => i.Dias!.Value)) })
                    .ToList()
            };
        }

        // ---------- 3. Equidad entre bebés ----------

        public EquidadBebesRespuesta ObtenerEquidadBebes()
        {
            var hoy = Hoy();
            var abrazos = AbrazosReales();
            var items = BebesActivos().Select(b =>
            {
                var ingreso = b.FechaIngresoNEO.HasValue ? DateOnly.FromDateTime(b.FechaIngresoNEO.Value) : hoy;
                var dias = Math.Max(1, hoy.DayNumber - ingreso.DayNumber + 1);
                var cantidad = abrazos.Count(a => a.IdBebe == b.ID);
                return new EquidadBebeItem
                {
                    IdBebe = b.ID,
                    Nombre = NombreBebe(b),
                    Sala = b.Sala?.Nombre,
                    DiasInternado = dias,
                    Abrazos = cantidad,
                    AbrazosPorSemana = R(cantidad * 7.0 / dias, 2)
                };
            }).ToList();

            var promedio = items.Count == 0 ? 0 : items.Average(i => i.AbrazosPorSemana);
            foreach (var i in items)
                i.PorDebajoDelPromedio = i.AbrazosPorSemana < promedio / 2;

            return new EquidadBebesRespuesta
            {
                PromedioAbrazosPorSemana = R(promedio, 2),
                BebesPorDebajo = items.Count(i => i.PorDebajoDelPromedio),
                Bebes = items.OrderBy(i => i.AbrazosPorSemana).ThenByDescending(i => i.DiasInternado).ToList()
            };
        }

        // ---------- 4. Cumplimiento del horario ----------

        public CumplimientoHorarioRespuesta ObtenerCumplimientoHorario(DateTime desde, DateTime hasta)
        {
            var d0 = DateOnly.FromDateTime(desde);
            var d1 = DateOnly.FromDateTime(hasta);
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(desde, hasta);
            var idElimVol = estadoRepositorio.ObtenerIdEstadoEliminado("Voluntarias");
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");

            var diasHorario = db.VOLUNTARIAHORARIO.AsNoTracking().Where(vh => vh.Activa)
                .Join(db.HORARIO.AsNoTracking(), vh => vh.IdHorario, h => h.IdHorario, (vh, h) => new { vh.IdVoluntaria, h.IdDia })
                .ToList()
                .GroupBy(x => x.IdVoluntaria)
                .ToDictionary(g => g.Key, g => g.Select(x => x.IdDia).Distinct().OrderBy(x => x).ToList());

            var vinieron = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.IdVoluntaria != null && a.FechaHoraIngreso != null
                            && a.FechaHoraIngreso >= inicioUtc && a.FechaHoraIngreso < finUtc
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => new { IdVoluntaria = a.IdVoluntaria!.Value, a.FechaHoraIngreso })
                .ToList()
                .GroupBy(a => a.IdVoluntaria)
                .ToDictionary(g => g.Key, g => g.Select(a => Dia(a.FechaHoraIngreso!.Value)).ToHashSet());

            var voluntarias = db.VOLUNTARIA.AsNoTracking()
                .Where(v => v.IdEstado != idElimVol && diasHorario.Keys.Contains(v.IdVoluntaria))
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido })
                .ToList();

            var items = voluntarias.Select(v =>
            {
                var dias = diasHorario[v.IdVoluntaria];
                var fechasPrevistas = new List<DateOnly>();
                for (var f = d0; f <= d1; f = f.AddDays(1))
                    if (dias.Contains(IdDia(f.DayOfWeek)))
                        fechasPrevistas.Add(f);
                var asistio = vinieron.GetValueOrDefault(v.IdVoluntaria) ?? new HashSet<DateOnly>();
                var cumplidos = fechasPrevistas.Count(asistio.Contains);
                return new CumplimientoVoluntariaItem
                {
                    IdVoluntaria = v.IdVoluntaria,
                    Nombre = $"{v.Nombre} {v.Apellido}".Trim(),
                    DiasDelHorario = string.Join(", ", dias.Select(NombreDia)),
                    DiasPrevistos = fechasPrevistas.Count,
                    DiasQueVino = cumplidos,
                    DiasExtra = asistio.Count(f => !fechasPrevistas.Contains(f)),
                    PorcentajeCumplimiento = fechasPrevistas.Count == 0 ? 0 : R(cumplidos * 100.0 / fechasPrevistas.Count)
                };
            }).Where(i => i.DiasPrevistos > 0).OrderBy(i => i.PorcentajeCumplimiento).ToList();

            return new CumplimientoHorarioRespuesta
            {
                FechaInicio = d0,
                FechaFin = d1,
                PromedioCumplimiento = items.Count == 0 ? null : R(items.Average(i => i.PorcentajeCumplimiento)),
                Voluntarias = items
            };
        }

        // ---------- 5. Proyección de insumos ----------

        public ProyeccionInsumosRespuesta ObtenerProyeccionInsumos(int diasAnalizados)
        {
            var desdeUtc = DateTime.UtcNow.AddDays(-diasAnalizados);
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Insumos");
            var salidas = db.MOVIMIENTOSTOCK.AsNoTracking()
                .Where(m => m.fechaMovimiento != null && m.fechaMovimiento >= desdeUtc && m.esEntrada != "S" && m.esEntrada != "s")
                .GroupBy(m => m.idInsumo)
                .Select(g => new { IdInsumo = g.Key, Cantidad = g.Sum(m => m.cantidad ?? 0) })
                .ToDictionary(x => x.IdInsumo, x => x.Cantidad);

            static int Prioridad(string e) => e switch
            {
                EstadosProyeccionInsumo.BajoMinimo => 0,
                EstadosProyeccionInsumo.Critico => 1,
                EstadosProyeccionInsumo.Atencion => 2,
                EstadosProyeccionInsumo.Ok => 3,
                _ => 4
            };

            var items = db.INSUMO.AsNoTracking().Where(i => i.idEstado != idElim).ToList().Select(i =>
            {
                var diario = salidas.GetValueOrDefault(i.idInsumo) / (double)diasAnalizados;
                double? hastaMin = diario > 0 ? R(Math.Max(0, i.stockActual - i.stockMinimo) / diario) : null;
                double? hastaCero = diario > 0 ? R(i.stockActual / diario) : null;
                var estado = i.stockActual <= i.stockMinimo ? EstadosProyeccionInsumo.BajoMinimo
                    : hastaMin == null ? EstadosProyeccionInsumo.SinConsumo
                    : hastaMin <= 7 ? EstadosProyeccionInsumo.Critico
                    : hastaMin <= 15 ? EstadosProyeccionInsumo.Atencion
                    : EstadosProyeccionInsumo.Ok;
                return new ProyeccionInsumoItem
                {
                    IdInsumo = i.idInsumo,
                    Nombre = i.nombre,
                    StockActual = i.stockActual,
                    StockMinimo = i.stockMinimo,
                    ConsumoDiarioPromedio = R(diario, 2),
                    DiasHastaMinimo = hastaMin,
                    DiasHastaAgotarse = hastaCero,
                    Estado = estado
                };
            }).OrderBy(i => Prioridad(i.Estado)).ThenBy(i => i.DiasHastaMinimo ?? double.MaxValue).ToList();

            return new ProyeccionInsumosRespuesta { DiasAnalizados = diasAnalizados, Insumos = items };
        }

        // ---------- 6. Mapa de calor ----------

        public MapaCalorRespuesta ObtenerMapaCalor(DateTime desde, DateTime hasta)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(desde, hasta);
            var abrazos = AbrazosReales(inicioUtc, finUtc).Select(a => NegConversorFecha.HoraArgentina(a.Inicio)).ToList();
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var asistencias = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioUtc && a.FechaHoraIngreso < finUtc
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => a.FechaHoraIngreso!.Value)
                .ToList()
                .Select(NegConversorFecha.HoraArgentina)
                .ToList();

            var celdas = abrazos.Select(h => (IdDia(h.DayOfWeek), h.Hour, Abrazo: true))
                .Concat(asistencias.Select(h => (IdDia(h.DayOfWeek), h.Hour, Abrazo: false)))
                .GroupBy(x => (x.Item1, x.Hour))
                .Select(g => new MapaCalorCelda
                {
                    IdDia = g.Key.Item1,
                    Dia = NombreDia(g.Key.Item1),
                    Hora = g.Key.Hour,
                    Abrazos = g.Count(x => x.Abrazo),
                    Asistencias = g.Count(x => !x.Abrazo)
                })
                .OrderBy(c => c.IdDia).ThenBy(c => c.Hora)
                .ToList();

            return new MapaCalorRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(desde),
                FechaFin = DateOnly.FromDateTime(hasta),
                TotalAbrazos = abrazos.Count,
                TotalAsistencias = asistencias.Count,
                Celdas = celdas
            };
        }

        // ---------- 7. Tasa de abrazos completados ----------

        private sealed record ResultadoAsignacion(int IdVoluntaria, string Voluntaria, int IdDiaSemana,
            bool Completado, bool CerradoAuto, bool NuncaIniciado, bool EnCurso);

        private static TasaCompletadosGrupoItem Grupo(string nombre, int? idVol, IEnumerable<ResultadoAsignacion> lista)
        {
            var l = lista.ToList();
            var completos = l.Count(x => x.Completado);
            return new TasaCompletadosGrupoItem
            {
                Grupo = nombre,
                IdVoluntaria = idVol,
                Total = l.Count,
                Completados = completos,
                CerradosAutomaticamente = l.Count(x => x.CerradoAuto),
                NuncaIniciados = l.Count(x => x.NuncaIniciado),
                PorcentajeCompletados = l.Count == 0 ? 0 : R(completos * 100.0 / l.Count)
            };
        }

        public TasaCompletadosRespuesta ObtenerTasaCompletados(DateTime desde, DateTime hasta)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(desde, hasta);
            var (inicioHoyUtc, _) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idElim = IdElimAsignaciones();
            var asignaciones = db.ASIGNACION.AsNoTracking()
                .Where(a => a.idBebe != null && a.idEstado != idElim
                            && a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtc)
                .Select(a => new
                {
                    a.idVoluntaria,
                    Vol = a.voluntaria != null ? a.voluntaria.Nombre + " " + a.voluntaria.Apellido : "",
                    a.fechaHoraAsignacion,
                    Iniciado = a.fechaHoraInicio != null,
                    Fin = a.fechaHoraFin != null,
                    a.cierreAutomatico
                })
                .ToList()
                // "Nunca iniciado": no se inició y ya se cerró, o es de un día anterior (las de hoy
                // sin iniciar todavía pueden iniciarse y no cuentan como perdidas).
                .Select(a => new ResultadoAsignacion(
                    a.idVoluntaria,
                    a.Vol.Trim(),
                    IdDia(NegConversorFecha.HoraArgentina(a.fechaHoraAsignacion).DayOfWeek),
                    Completado: a.Iniciado && a.Fin && !a.cierreAutomatico,
                    CerradoAuto: a.Iniciado && a.cierreAutomatico,
                    NuncaIniciado: !a.Iniciado && (a.Fin || a.fechaHoraAsignacion < inicioHoyUtc),
                    EnCurso: a.Iniciado && !a.Fin))
                .ToList();

            var total = asignaciones.Count;
            var completados = asignaciones.Count(a => a.Completado);
            return new TasaCompletadosRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(desde),
                FechaFin = DateOnly.FromDateTime(hasta),
                Total = total,
                Completados = completados,
                CerradosAutomaticamente = asignaciones.Count(a => a.CerradoAuto),
                NuncaIniciados = asignaciones.Count(a => a.NuncaIniciado),
                EnCurso = asignaciones.Count(a => a.EnCurso),
                PorcentajeCompletados = total == 0 ? 0 : R(completados * 100.0 / total),
                PorVoluntaria = asignaciones.GroupBy(a => (a.IdVoluntaria, a.Voluntaria))
                    .Select(g => Grupo(g.Key.Voluntaria, g.Key.IdVoluntaria, g))
                    .OrderBy(g => g.PorcentajeCompletados).ThenByDescending(g => g.Total)
                    .ToList(),
                PorDiaSemana = asignaciones.GroupBy(a => a.IdDiaSemana).OrderBy(g => g.Key)
                    .Select(g => Grupo(NombreDia(g.Key), null, g))
                    .ToList()
            };
        }

        // ---------- 8. Bebés sin visitas ----------

        public BebesSinVisitasRespuesta ObtenerBebesSinVisitas(int umbralDias)
        {
            var hoy = Hoy();
            var ultimas = db.VISITA.AsNoTracking().Where(v => v.Activa)
                .GroupBy(v => v.idBebe)
                .Select(g => new { IdBebe = g.Key, Ultima = g.Max(v => v.fechaHoraVisita) })
                .ToDictionary(x => x.IdBebe, x => x.Ultima);

            var items = BebesActivos().Select(b =>
            {
                DateTime? ultima = ultimas.TryGetValue(b.ID, out var u) ? u : null;
                DateOnly? referencia = ultima.HasValue ? Dia(ultima.Value)
                    : b.FechaIngresoNEO.HasValue ? DateOnly.FromDateTime(b.FechaIngresoNEO.Value) : null;
                return new BebeSinVisitasItem
                {
                    IdBebe = b.ID,
                    Nombre = NombreBebe(b),
                    Sala = b.Sala?.Nombre,
                    UltimaVisita = ultima,
                    DiasSinVisita = referencia.HasValue ? hoy.DayNumber - referencia.Value.DayNumber : null,
                    NuncaVisitado = ultima == null
                };
            }).ToList();

            var superan = items.Where(i => i.DiasSinVisita == null || i.DiasSinVisita >= umbralDias)
                .OrderByDescending(i => i.DiasSinVisita ?? int.MaxValue).ToList();
            return new BebesSinVisitasRespuesta { UmbralDias = umbralDias, BebesSuperanUmbral = superan.Count, Bebes = superan };
        }

        // ---------- 9. Rendimiento de voluntarias ----------

        public RendimientoVoluntariasRespuesta ObtenerRendimientoVoluntarias(DateTime desde, DateTime hasta)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(desde, hasta);
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var horas = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.IdVoluntaria != null && a.FechaHoraIngreso != null && a.FechaHoraSalida != null
                            && a.FechaHoraIngreso >= inicioUtc && a.FechaHoraIngreso < finUtc
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => new { IdVoluntaria = a.IdVoluntaria!.Value, a.FechaHoraIngreso, a.FechaHoraSalida })
                .ToList()
                .Select(a => new { a.IdVoluntaria, Duracion = a.FechaHoraSalida!.Value - a.FechaHoraIngreso!.Value })
                .Where(a => a.Duracion > TimeSpan.Zero && a.Duracion <= JornadaMaxima)
                .GroupBy(a => a.IdVoluntaria)
                .ToDictionary(g => g.Key, g => g.Sum(a => a.Duracion.TotalHours));

            var abrazos = AbrazosReales(inicioUtc, finUtc).GroupBy(a => a.IdVoluntaria)
                .ToDictionary(g => g.Key, g => (Cantidad: g.Count(), Minutos: g.Sum(a => a.Minutos)));

            // Solo voluntarias que siguen activas: las dadas de baja (incluidas las de prueba) distorsionan el ranking.
            var idElimVol = estadoRepositorio.ObtenerIdEstadoEliminado("Voluntarias");
            var candidatas = horas.Keys.Union(abrazos.Keys).ToList();
            var nombres = db.VOLUNTARIA.AsNoTracking()
                .Where(v => candidatas.Contains(v.IdVoluntaria) && v.IdEstado != idElimVol)
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido })
                .ToDictionary(v => v.IdVoluntaria, v => $"{v.Nombre} {v.Apellido}".Trim());
            var ids = nombres.Keys.ToList();

            var items = ids.Select(id =>
            {
                var h = horas.GetValueOrDefault(id);
                var (cant, min) = abrazos.GetValueOrDefault(id);
                return new RendimientoVoluntariaItem
                {
                    IdVoluntaria = id,
                    Nombre = nombres.GetValueOrDefault(id) ?? $"Voluntaria #{id}",
                    HorasEnHospital = R(h, 2),
                    Abrazos = cant,
                    MinutosAbrazando = R(min),
                    // Con menos de 1 hora acumulada los cocientes se disparan y no dicen nada.
                    AbrazosPorHora = h >= 1 ? R(cant / h, 2) : null,
                    PorcentajeTiempoAbrazando = h >= 1 ? R(Math.Min(100, min / (h * 60) * 100)) : null
                };
            }).OrderByDescending(i => i.AbrazosPorHora ?? -1).ThenByDescending(i => i.Abrazos).ToList();

            return new RendimientoVoluntariasRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(desde),
                FechaFin = DateOnly.FromDateTime(hasta),
                Aclaracion = "Horas en el hospital = asistencias con entrada y salida (se descartan las de más de 12 h o sin salida). Abrazos = abrazos reales iniciados en el período. Abrazos por hora y % de tiempo se calculan con al menos 1 hora acumulada. Solo voluntarias activas.",
                Voluntarias = items
            };
        }

        // ---------- 10. Duración por sala y por peso ----------

        public DuracionPorGrupoRespuesta ObtenerDuracionPorGrupo(DateTime desde, DateTime hasta)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(desde, hasta);
            var abrazos = AbrazosReales(inicioUtc, finUtc);
            var ids = abrazos.Select(a => a.IdBebe).Distinct().ToList();
            var bebes = db.BEBE.AsNoTracking().Include(b => b.Sala).Where(b => ids.Contains(b.ID)).ToDictionary(b => b.ID);

            static string TramoPeso(double? g) => g switch
            {
                null => "Sin peso cargado",
                < 1500 => "Menos de 1,5 kg",
                < 2500 => "1,5 a 2,5 kg",
                _ => "2,5 kg o más"
            };

            List<DuracionGrupoItem> Agrupar(Func<AbrazoReal, string> clave) => abrazos
                .GroupBy(clave)
                .Select(g => new DuracionGrupoItem
                {
                    Grupo = g.Key,
                    Abrazos = g.Count(),
                    PromedioMinutos = R(g.Average(a => a.Minutos)),
                    MedianaMinutos = R(Mediana(g.Select(a => a.Minutos).ToList()))
                })
                .OrderByDescending(g => g.Abrazos)
                .ToList();

            return new DuracionPorGrupoRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(desde),
                FechaFin = DateOnly.FromDateTime(hasta),
                PorSala = Agrupar(a => bebes.TryGetValue(a.IdBebe, out var b) ? b.Sala?.Nombre ?? "Sin sala" : "Sin sala"),
                PorPeso = Agrupar(a => bebes.TryGetValue(a.IdBebe, out var b) ? TramoPeso(PesoEnGramos(b.PesoDiaAbrazos) ?? PesoEnGramos(b.PesoIngresoNEO)) : "Sin peso cargado")
            };
        }
    }
}
