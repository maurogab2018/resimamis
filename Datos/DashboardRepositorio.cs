using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class DashboardRepositorio : IDashboardRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;
        private readonly IBebeRepositorio bebeRepositorio;

        private static readonly (string Etiqueta, int Min, int? Max)[] RangosEdadBebes =
        {
            ("0-7 días", 0, 7),
            ("8-14 días", 8, 14),
            ("15-28 días", 15, 28),
            ("29-60 días", 29, 60),
            ("61+ días", 61, null)
        };

        public DashboardRepositorio(
            ApplicationDbContext db,
            IEstadoRepositorio estadoRepositorio,
            IBebeRepositorio bebeRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
            this.bebeRepositorio = bebeRepositorio;
        }

        private int IdEstadoEliminadoAsignaciones() =>
            estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

        private int? IdEstadoEliminadoBebes()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Bebes")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();
        }

        private IQueryable<ASIGNACION> QueryAsignacionesActivas() =>
            db.ASIGNACION.AsNoTracking()
                .Where(a => a.idEstado != IdEstadoEliminadoAsignaciones());

        private IQueryable<BEBE> QueryBebesActivos()
        {
            var idElim = IdEstadoEliminadoBebes();
            var q = db.BEBE.AsNoTracking()
                .Include(b => b.Estado!)
                .ThenInclude(e => e!.ambito)
                .Include(b => b.Sala)
                .Where(b => b.FechaSalida == null)
                .AsQueryable();
            if (idElim != null)
                q = q.Where(b => b.IdEstado != idElim);
            return q;
        }

        public EstadisticaAsignacionesPorDiaRespuesta ObtenerAsignacionesPorDia(DateTime fechaInicio, DateTime fechaFin)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var asignaciones = QueryAsignacionesActivas()
                .Where(a => a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtc)
                .Select(a => new { a.fechaHoraAsignacion, a.idBebe })
                .ToList();

            var porDia = asignaciones
                .GroupBy(a => NegConversorFecha.FechaCalendarioArgentina(a.fechaHoraAsignacion))
                .Select(g => new EstadisticaAsignacionesPorDiaItem
                {
                    Fecha = g.Key,
                    CantidadAsignaciones = g.Count(),
                    CantidadAbrazos = g.Count(x => x.idBebe != null)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            return new EstadisticaAsignacionesPorDiaRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                TotalAsignaciones = asignaciones.Count,
                TotalAbrazos = asignaciones.Count(a => a.idBebe != null),
                PorDia = porDia
            };
        }

        public EstadisticaDuracionAbrazosRespuesta ObtenerDuracionAbrazos(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var q = QueryAsignacionesActivas()
                .Where(a => a.idBebe != null
                            && a.fechaHoraInicio != null
                            && a.fechaHoraFin != null
                            && !a.cierreAutomatico);

            DateOnly? dInicio = null;
            DateOnly? dFin = null;
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio.Value, fechaFin.Value);
                q = q.Where(a => a.fechaHoraFin >= inicioUtc && a.fechaHoraFin < finUtc);
                dInicio = DateOnly.FromDateTime(fechaInicio.Value.Date);
                dFin = DateOnly.FromDateTime(fechaFin.Value.Date);
            }

            var abrazos = q
                .Select(a => new { a.fechaHoraInicio, a.fechaHoraFin })
                .ToList();

            var duraciones = abrazos
                .Select(a => (a.fechaHoraFin!.Value - a.fechaHoraInicio!.Value).TotalMinutes)
                .ToList();

            var respuesta = new EstadisticaDuracionAbrazosRespuesta
            {
                FechaInicio = dInicio,
                FechaFin = dFin,
                CantidadAbrazosFinalizados = duraciones.Count
            };

            if (duraciones.Count == 0)
                return respuesta;

            respuesta.PromedioMinutos = duraciones.Average();
            respuesta.MinimoMinutos = duraciones.Min();
            respuesta.MaximoMinutos = duraciones.Max();
            respuesta.TotalMinutos = duraciones.Sum();
            return respuesta;
        }

        public EstadisticaRangoEdadesBebesRespuesta ObtenerRangoEdadesBebes()
        {
            var bebes = QueryBebesActivos()
                .Where(b => b.FechaNacimiento != null)
                .ToList();

            var edades = bebes
                .Select(b => NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(b.FechaNacimiento!.Value))
                .ToList();

            var rangos = RangosEdadBebes.Select(r => new EstadisticaRangoEdadBebeItem
            {
                Rango = r.Etiqueta,
                EdadMinDias = r.Min,
                EdadMaxDias = r.Max,
                CantidadBebes = edades.Count(e => r.Max == null ? e >= r.Min : e >= r.Min && e <= r.Max)
            }).ToList();

            return new EstadisticaRangoEdadesBebesRespuesta
            {
                TotalBebes = bebes.Count,
                Rangos = rangos
            };
        }

        public EstadisticaPermanenciaBebesRespuesta ObtenerPermanenciaBebes()
        {
            var bebes = QueryBebesActivos()
                .Where(b => b.FechaIngresoNEO != null)
                .OrderBy(b => b.apellido)
                .ThenBy(b => b.nombre)
                .ToList();

            var items = bebes.Select(b =>
            {
                var dias = NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(b.FechaIngresoNEO!.Value);
                return new EstadisticaPermanenciaBebeItem
                {
                    IdBebe = b.ID,
                    Nombre = b.nombre,
                    Apellido = b.apellido,
                    FechaIngresoNeo = b.FechaIngresoNEO,
                    DiasPermanencia = dias,
                    EstadoBebe = b.Estado?.nombre,
                    NombreSala = b.Sala?.Nombre
                };
            }).ToList();

            var respuesta = new EstadisticaPermanenciaBebesRespuesta
            {
                TotalBebes = items.Count,
                Bebes = items
            };

            if (items.Count == 0)
                return respuesta;

            respuesta.PromedioDias = items.Average(i => i.DiasPermanencia);
            respuesta.MinimoDias = items.Min(i => i.DiasPermanencia);
            respuesta.MaximoDias = items.Max(i => i.DiasPermanencia);
            return respuesta;
        }

        public EstadisticaVisitasRespuesta ObtenerEstadisticasVisitas(DateTime fechaInicio, DateTime fechaFin, int? idBebe = null)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var visitas = db.VISITA.AsNoTracking()
                .Where(v => v.Activa
                            && v.fechaHoraVisita >= inicioUtc && v.fechaHoraVisita < finUtc
                            && (idBebe == null || v.idBebe == idBebe))
                .Select(v => new
                {
                    v.idBebe,
                    v.familiar,
                    v.fechaHoraVisita,
                    nombreBebe = v.Bebe != null ? v.Bebe.nombre : null,
                    apellidoBebe = v.Bebe != null ? v.Bebe.apellido : null
                })
                .ToList();

            var porBebe = visitas
                .GroupBy(v => v.idBebe)
                .Select(g => new EstadisticaVisitasPorBebeItem
                {
                    IdBebe = g.Key,
                    NombreBebe = $"{g.First().nombreBebe} {g.First().apellidoBebe}".Trim(),
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.NombreBebe)
                .ToList();

            var porDia = visitas
                .GroupBy(v => NegConversorFecha.FechaCalendarioArgentina(v.fechaHoraVisita))
                .Select(g => new EstadisticaVisitasPorDiaItem
                {
                    Fecha = g.Key,
                    Cantidad = g.Count()
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            var porFamiliar = visitas
                .GroupBy(v => string.IsNullOrWhiteSpace(v.familiar) ? "Sin especificar" : v.familiar.Trim())
                .Select(g => new EstadisticaVisitasPorFamiliarItem
                {
                    Familiar = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            return new EstadisticaVisitasRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                IdBebe = idBebe,
                TotalVisitas = visitas.Count,
                BebesVisitados = visitas.Select(v => v.idBebe).Distinct().Count(),
                PorDia = porDia,
                PorFamiliar = porFamiliar,
                PorBebe = porBebe
            };
        }

        public DashboardResumenRespuesta ObtenerResumen(DateTime fechaInicio, DateTime fechaFin)
        {
            var asignaciones = ObtenerAsignacionesPorDia(fechaInicio, fechaFin);
            var duracion = ObtenerDuracionAbrazos(fechaInicio, fechaFin);
            var visitas = ObtenerEstadisticasVisitas(fechaInicio, fechaFin);
            var permanencia = ObtenerPermanenciaBebes();
            var bebesActivos = QueryBebesActivos().Count();
            var bebesDisponibles = bebeRepositorio.obtenerBebesAbrazar().Count;

            return new DashboardResumenRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                AsignacionesEnPeriodo = asignaciones.TotalAsignaciones,
                AbrazosFinalizadosEnPeriodo = duracion.CantidadAbrazosFinalizados,
                VisitasEnPeriodo = visitas.TotalVisitas,
                BebesActivos = bebesActivos,
                BebesDisponiblesAbrazo = bebesDisponibles,
                PromedioDuracionAbrazoMinutos = duracion.PromedioMinutos,
                PromedioPermanenciaDias = permanencia.PromedioDias
            };
        }

        public AbrazosBebeDashboardRespuesta ObtenerAbrazosBebe(
            int idBebe,
            DateTime? inicioUtc,
            DateTime? finUtcExclusivo,
            DateOnly? fechaConsulta)
        {
            var bebe = bebeRepositorio.consultarBebe(idBebe);
            var q = QueryAsignacionesActivas()
                .Include(a => a.voluntaria)
                .Include(a => a.estado)
                .Where(a => a.idBebe == idBebe);

            if (inicioUtc.HasValue && finUtcExclusivo.HasValue)
                q = q.Where(a => a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtcExclusivo);

            var asignaciones = q
                .OrderByDescending(a => a.fechaHoraAsignacion)
                .ThenByDescending(a => a.idAsignacion)
                .ToList();

            var abrazos = asignaciones.Select(a =>
            {
                double? minutos = null;
                if (a.fechaHoraInicio.HasValue && a.fechaHoraFin.HasValue && !a.cierreAutomatico)
                    minutos = (a.fechaHoraFin.Value - a.fechaHoraInicio.Value).TotalMinutes;

                return new AbrazoBebeDashboardItem
                {
                    IdAsignacion = a.idAsignacion,
                    FechaHoraAsignacion = a.fechaHoraAsignacion,
                    FechaHoraInicio = a.fechaHoraInicio,
                    FechaHoraFin = a.fechaHoraFin,
                    DuracionMinutos = minutos,
                    EstadoAsignacion = a.estado?.nombre ?? a.idEstado.ToString(),
                    NombreVoluntaria = FormatearNombre(a.voluntaria?.Nombre, a.voluntaria?.Apellido),
                    Comentario = a.comentario,
                    CierreAutomatico = a.cierreAutomatico
                };
            }).ToList();

            return new AbrazosBebeDashboardRespuesta
            {
                IdBebe = bebe.ID,
                NombreBebe = bebe.nombre,
                ApellidoBebe = bebe.apellido,
                FechaConsulta = fechaConsulta,
                FechaInicio = inicioUtc.HasValue ? NegConversorFecha.FechaCalendarioArgentina(inicioUtc.Value) : null,
                FechaFin = finUtcExclusivo.HasValue
                    ? NegConversorFecha.FechaCalendarioArgentina(finUtcExclusivo.Value.AddTicks(-1))
                    : null,
                TotalAbrazos = abrazos.Count,
                AbrazosFinalizados = abrazos.Count(a => a.FechaHoraInicio.HasValue && a.FechaHoraFin.HasValue && !a.CierreAutomatico),
                Abrazos = abrazos
            };
        }

        public AbrazosVoluntariaDashboardRespuesta ObtenerAbrazosVoluntaria(
            int idVoluntaria,
            DateTime? inicioUtc,
            DateTime? finUtcExclusivo,
            DateOnly? fechaConsulta)
        {
            var voluntaria = db.VOLUNTARIA.AsNoTracking()
                .FirstOrDefault(v => v.IdVoluntaria == idVoluntaria);
            if (voluntaria == null)
                throw new NotFoundException("Voluntaria no encontrada con ese Id.");

            var q = QueryAsignacionesActivas()
                .Include(a => a.bebe)
                .Include(a => a.estado)
                .Where(a => a.idVoluntaria == idVoluntaria && a.idBebe != null);

            if (inicioUtc.HasValue && finUtcExclusivo.HasValue)
                q = q.Where(a => a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtcExclusivo);

            var asignaciones = q
                .OrderByDescending(a => a.fechaHoraAsignacion)
                .ThenByDescending(a => a.idAsignacion)
                .Take(50)
                .ToList();

            var abrazos = asignaciones.Select(a =>
            {
                double? minutos = null;
                if (a.fechaHoraInicio.HasValue && a.fechaHoraFin.HasValue && !a.cierreAutomatico)
                    minutos = (a.fechaHoraFin.Value - a.fechaHoraInicio.Value).TotalMinutes;

                return new AbrazoVoluntariaDashboardItem
                {
                    IdAsignacion = a.idAsignacion,
                    IdBebe = a.idBebe,
                    NombreBebe = a.bebe?.nombre,
                    ApellidoBebe = a.bebe?.apellido,
                    FechaHoraAsignacion = a.fechaHoraAsignacion,
                    FechaHoraInicio = a.fechaHoraInicio,
                    FechaHoraFin = a.fechaHoraFin,
                    DuracionMinutos = minutos,
                    EstadoAsignacion = a.estado?.nombre ?? a.idEstado.ToString(),
                    Comentario = a.comentario,
                    CierreAutomatico = a.cierreAutomatico
                };
            }).ToList();

            return new AbrazosVoluntariaDashboardRespuesta
            {
                IdVoluntaria = voluntaria.IdVoluntaria,
                NombreVoluntaria = FormatearNombre(voluntaria.Nombre, voluntaria.Apellido),
                FechaConsulta = fechaConsulta,
                FechaInicio = inicioUtc.HasValue ? NegConversorFecha.FechaCalendarioArgentina(inicioUtc.Value) : null,
                FechaFin = finUtcExclusivo.HasValue
                    ? NegConversorFecha.FechaCalendarioArgentina(finUtcExclusivo.Value.AddTicks(-1))
                    : null,
                TotalAbrazos = abrazos.Count,
                AbrazosFinalizados = abrazos.Count(a => a.FechaHoraInicio.HasValue && a.FechaHoraFin.HasValue && !a.CierreAutomatico),
                Abrazos = abrazos
            };
        }

        private static string FormatearNombre(string? nombre, string? apellido)
        {
            var n = nombre?.Trim() ?? "";
            var a = apellido?.Trim() ?? "";
            if (n.Length == 0 && a.Length == 0) return "";
            if (n.Length == 0) return a;
            if (a.Length == 0) return n;
            return $"{n} {a}";
        }

        public DashboardCoordinacionHoyRespuesta ObtenerCoordinacionHoy(
            DateTime inicioUtc,
            DateTime finUtcExclusivo,
            DateOnly fecha)
        {
            var bebesActivos = QueryBebesActivos().ToList();
            var bebesDisponibles = bebeRepositorio.obtenerBebesAbrazar().Count;
            var bebesAsignados = bebesActivos.Count(b => b.Estado?.nombre == "Asignado");

            var abrazosHoy = QueryAsignacionesActivas()
                .Where(a => a.idBebe != null && a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtcExclusivo)
                .Select(a => new { a.fechaHoraInicio, a.fechaHoraFin })
                .ToList();

            var abrazosEnCursoHoy = abrazosHoy.Count(a =>
                a.fechaHoraInicio != null
                && a.fechaHoraInicio >= inicioUtc
                && a.fechaHoraInicio < finUtcExclusivo
                && a.fechaHoraFin == null);

            var abrazosFinalizadosHoy = QueryAsignacionesActivas()
                .Count(a => a.idBebe != null
                            && a.fechaHoraFin != null && !a.cierreAutomatico
                            && a.fechaHoraFin >= inicioUtc
                            && a.fechaHoraFin < finUtcExclusivo);

            var abrazosColgados = QueryAsignacionesActivas()
                .Count(a => a.idBebe != null && a.fechaHoraInicio != null && a.fechaHoraFin == null);

            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var voluntariasConAsistencia = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.FechaHoraIngreso != null
                            && a.FechaHoraIngreso >= inicioUtc
                            && a.FechaHoraIngreso < finUtcExclusivo
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => a.IdVoluntaria)
                .Distinct()
                .Count();

            var visitasHoy = db.VISITA.AsNoTracking()
                .Count(v => v.Activa && v.fechaHoraVisita >= inicioUtc && v.fechaHoraVisita < finUtcExclusivo);

            return new DashboardCoordinacionHoyRespuesta
            {
                Fecha = fecha,
                BebesActivos = bebesActivos.Count,
                BebesDisponiblesAbrazo = bebesDisponibles,
                BebesAsignados = bebesAsignados,
                AbrazosHoy = new AbrazosHoyResumen
                {
                    Creados = abrazosHoy.Count,
                    EnCurso = abrazosEnCursoHoy,
                    Finalizados = abrazosFinalizadosHoy
                },
                VoluntariasConAsistenciaHoy = voluntariasConAsistencia,
                AbrazosColgados = abrazosColgados,
                VisitasHoy = visitasHoy
            };
        }

        public DashboardCoberturaHoyRespuesta ObtenerCoberturaHoy(
            DateTime inicioUtc,
            DateTime finUtcExclusivo,
            DateOnly fecha)
        {
            var bebesActivos = QueryBebesActivos()
                .OrderBy(b => b.apellido)
                .ThenBy(b => b.nombre)
                .ToList();

            var bebesConAbrazoHoy = QueryAsignacionesActivas()
                .Where(a => a.idBebe != null
                            && a.fechaHoraFin != null && !a.cierreAutomatico
                            && a.fechaHoraFin >= inicioUtc
                            && a.fechaHoraFin < finUtcExclusivo)
                .Select(a => a.idBebe!.Value)
                .Distinct()
                .ToHashSet();

            var conAbrazo = bebesConAbrazoHoy.Count;
            var total = bebesActivos.Count;
            var sinAbrazo = bebesActivos
                .Where(b => !bebesConAbrazoHoy.Contains(b.ID))
                .Select(b => new BebeSinAbrazoHoyItem
                {
                    IdBebe = b.ID,
                    Nombre = b.nombre,
                    Apellido = b.apellido,
                    EstadoBebe = b.Estado?.nombre,
                    NombreSala = b.Sala?.Nombre
                })
                .ToList();

            return new DashboardCoberturaHoyRespuesta
            {
                Fecha = fecha,
                TotalBebesActivos = total,
                BebesConAbrazoFinalizadoHoy = conAbrazo,
                PorcentajeCobertura = total == 0 ? 0 : Math.Round(conAbrazo * 100.0 / total, 2),
                BebesSinAbrazoHoy = sinAbrazo
            };
        }

        public EstadisticaBebesPorEstadoRespuesta ObtenerBebesPorEstado()
        {
            var porEstado = QueryBebesActivos()
                .GroupBy(b => b.Estado != null ? b.Estado.nombre : "Sin estado")
                .Select(g => new EstadisticaBebesPorEstadoItem
                {
                    EstadoBebe = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.EstadoBebe)
                .ToList();

            return new EstadisticaBebesPorEstadoRespuesta
            {
                TotalBebes = porEstado.Sum(x => x.Cantidad),
                PorEstado = porEstado
            };
        }

        public EstadisticaBebesPorSalaRespuesta ObtenerBebesPorSala()
        {
            var bebes = QueryBebesActivos().ToList();

            var porSala = bebes
                .GroupBy(b => new { b.IdSala, Nombre = b.Sala?.Nombre ?? "Sin sala" })
                .Select(g =>
                {
                    var conPermanencia = g.Where(b => b.FechaIngresoNEO != null).ToList();
                    return new EstadisticaBebesPorSalaItem
                    {
                        IdSala = g.Key.IdSala,
                        NombreSala = g.Key.Nombre,
                        CantidadBebes = g.Count(),
                        PromedioPermanenciaDias = conPermanencia.Count == 0
                            ? 0
                            : conPermanencia.Average(b =>
                                NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(b.FechaIngresoNEO!.Value))
                    };
                })
                .OrderByDescending(x => x.CantidadBebes)
                .ThenBy(x => x.NombreSala)
                .ToList();

            return new EstadisticaBebesPorSalaRespuesta
            {
                TotalBebes = bebes.Count,
                PorSala = porSala
            };
        }

        public RankingVoluntariasAbrazosRespuesta ObtenerRankingVoluntariasAbrazos(
            DateTime fechaInicio,
            DateTime fechaFin,
            int top)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);

            var ranking = QueryAsignacionesActivas()
                .Include(a => a.voluntaria)
                .Where(a => a.idBebe != null
                            && a.fechaHoraInicio != null
                            && a.fechaHoraFin != null && !a.cierreAutomatico
                            && a.fechaHoraFin >= inicioUtc
                            && a.fechaHoraFin < finUtc)
                .GroupBy(a => new
                {
                    a.idVoluntaria,
                    a.voluntaria!.Nombre,
                    a.voluntaria.Apellido
                })
                .Select(g => new
                {
                    g.Key.idVoluntaria,
                    g.Key.Nombre,
                    g.Key.Apellido,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Apellido)
                .ThenBy(x => x.Nombre)
                .Take(top)
                .ToList();

            var items = ranking.Select((x, i) => new RankingVoluntariaAbrazosItem
            {
                Posicion = i + 1,
                IdVoluntaria = x.idVoluntaria,
                NombreVoluntaria = FormatearNombre(x.Nombre, x.Apellido),
                CantidadAbrazosFinalizados = x.Cantidad
            }).ToList();

            return new RankingVoluntariasAbrazosRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                Top = top,
                Ranking = items
            };
        }

        /// <summary>
        /// Evolución de peso ingreso NEO vs egreso (PesoAlta).
        /// Con fechas: filtra egresos (FechaSalida) en el período; si no hay salida, usa FechaIngresoNEO.
        /// Sin fechas: todos los bebés con al menos un peso cargado.
        /// </summary>
        public EvolucionPesoBebesRespuesta ObtenerEvolucionPesoBebes(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var q = db.BEBE.AsNoTracking()
                .Include(b => b.Sala)
                .Where(b => b.PesoIngresoNEO != null
                            || b.PesoAlta != null
                            || b.PesoNacimiento != null
                            || b.PesoDiaAbrazos != null);

            DateOnly? desde = null;
            DateOnly? hasta = null;
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio.Value, fechaFin.Value);
                desde = DateOnly.FromDateTime(fechaInicio.Value.Date);
                hasta = DateOnly.FromDateTime(fechaFin.Value.Date);
                q = q.Where(b =>
                    (b.FechaSalida != null && b.FechaSalida >= inicioUtc && b.FechaSalida < finUtc)
                    || (b.FechaSalida == null
                        && b.FechaIngresoNEO != null
                        && b.FechaIngresoNEO >= inicioUtc
                        && b.FechaIngresoNEO < finUtc));
            }

            var raw = q
                .OrderByDescending(b => b.FechaSalida ?? b.FechaIngresoNEO)
                .ThenBy(b => b.apellido)
                .ThenBy(b => b.nombre)
                .ToList();

            var items = raw.Select(b =>
            {
                decimal? diferencia = null;
                double? porcentaje = null;
                var completa = b.PesoIngresoNEO.HasValue && b.PesoAlta.HasValue;
                if (completa)
                {
                    diferencia = b.PesoAlta!.Value - b.PesoIngresoNEO!.Value;
                    if (b.PesoIngresoNEO.Value != 0)
                        porcentaje = (double)(diferencia.Value / b.PesoIngresoNEO.Value * 100m);
                }

                return new EvolucionPesoBebeItem
                {
                    IdBebe = b.ID,
                    Nombre = b.nombre,
                    Apellido = b.apellido,
                    NombreSala = b.Sala?.Nombre,
                    FechaIngresoNeo = b.FechaIngresoNEO,
                    FechaSalida = b.FechaSalida,
                    PesoNacimiento = b.PesoNacimiento,
                    PesoIngresoNeo = b.PesoIngresoNEO,
                    PesoDiaAbrazos = b.PesoDiaAbrazos,
                    PesoEgreso = b.PesoAlta,
                    DiferenciaIngresoEgreso = diferencia,
                    PorcentajeVariacion = porcentaje.HasValue
                        ? Math.Round(porcentaje.Value, 2)
                        : null,
                    TieneComparacionCompleta = completa
                };
            }).ToList();

            var conComparacion = items.Where(x => x.TieneComparacionCompleta).ToList();
            decimal? promIngreso = null;
            decimal? promEgreso = null;
            decimal? promDiff = null;
            decimal? gananciaMin = null;
            decimal? gananciaMax = null;
            if (conComparacion.Count > 0)
            {
                promIngreso = Math.Round(conComparacion.Average(x => x.PesoIngresoNeo!.Value), 2);
                promEgreso = Math.Round(conComparacion.Average(x => x.PesoEgreso!.Value), 2);
                promDiff = Math.Round(conComparacion.Average(x => x.DiferenciaIngresoEgreso!.Value), 2);
                gananciaMin = Math.Round(conComparacion.Min(x => x.DiferenciaIngresoEgreso!.Value), 2);
                gananciaMax = Math.Round(conComparacion.Max(x => x.DiferenciaIngresoEgreso!.Value), 2);
            }

            return new EvolucionPesoBebesRespuesta
            {
                FechaInicio = desde,
                FechaFin = hasta,
                TotalBebes = items.Count,
                BebesConComparacionCompleta = conComparacion.Count,
                BebesConGanancia = conComparacion.Count(x => x.DiferenciaIngresoEgreso > 0),
                BebesConPerdida = conComparacion.Count(x => x.DiferenciaIngresoEgreso < 0),
                BebesSinCambio = conComparacion.Count(x => x.DiferenciaIngresoEgreso == 0),
                PromedioPesoIngreso = promIngreso,
                PromedioPesoEgreso = promEgreso,
                PromedioDiferencia = promDiff,
                GananciaMinima = gananciaMin,
                GananciaMaxima = gananciaMax,
                Bebes = items
            };
        }

        /// <summary>
        /// Insumos para recomendar a coordinación: qué bebés llevan más tiempo sin abrazo y qué
        /// voluntarias activas dejaron de venir. Los abrazos cerrados automáticamente no cuentan
        /// como abrazo recibido.
        /// </summary>
        public AlertasCoordinacionRespuesta ObtenerAlertasCoordinacion(int topBebes, int diasUmbralVoluntarias)
        {
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            int Dias(DateTime d) => hoy.DayNumber - NegConversorFecha.FechaCalendarioArgentina(d).DayNumber;

            var ultimoAbrazo = QueryAsignacionesActivas()
                .Where(a => a.idBebe != null && a.fechaHoraFin != null && !a.cierreAutomatico)
                .GroupBy(a => a.idBebe!.Value)
                .Select(g => new { IdBebe = g.Key, Ultimo = g.Max(a => a.fechaHoraFin!.Value) })
                .ToDictionary(x => x.IdBebe, x => x.Ultimo);

            var bebes = QueryBebesActivos()
                .Select(b => new { b.ID, b.nombre, b.apellido, Sala = b.Sala != null ? b.Sala.Nombre : null, b.FechaIngresoNEO })
                .ToList()
                .Select(b =>
                {
                    var ultimo = ultimoAbrazo.TryGetValue(b.ID, out var u) ? u : (DateTime?)null;
                    var referencia = ultimo ?? b.FechaIngresoNEO;
                    return new BebeSinAbrazoItem
                    {
                        IdBebe = b.ID,
                        Nombre = $"{b.nombre} {b.apellido}".Trim(),
                        Sala = b.Sala,
                        UltimoAbrazo = ultimo,
                        DiasSinAbrazo = referencia.HasValue ? Dias(referencia.Value) : null
                    };
                })
                .OrderByDescending(b => b.DiasSinAbrazo ?? int.MaxValue)
                .ThenBy(b => b.UltimoAbrazo.HasValue)
                .Take(topBebes)
                .ToList();

            var idElimVol = estadoRepositorio.ObtenerIdEstadoEliminado("Voluntarias");
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var ultimaAsistencia = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.IdVoluntaria != null && a.FechaHoraIngreso != null && (a.idEstado == null || a.idEstado != idElimAsist))
                .GroupBy(a => a.IdVoluntaria!.Value)
                .Select(g => new { Id = g.Key, Ultima = g.Max(a => a.FechaHoraIngreso!.Value) })
                .ToDictionary(x => x.Id, x => x.Ultima);

            var voluntarias = db.VOLUNTARIA.AsNoTracking()
                .Where(v => v.IdEstado != idElimVol)
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido, v.FechaInicio })
                .ToList()
                .Select(v =>
                {
                    var ultima = ultimaAsistencia.TryGetValue(v.IdVoluntaria, out var u) ? u : (DateTime?)null;
                    return new VoluntariaSinVenirItem
                    {
                        IdVoluntaria = v.IdVoluntaria,
                        Nombre = $"{v.Nombre} {v.Apellido}".Trim(),
                        UltimaAsistencia = ultima,
                        DiasSinVenir = Dias(ultima ?? v.FechaInicio)
                    };
                })
                .Where(v => v.DiasSinVenir >= diasUmbralVoluntarias)
                .OrderByDescending(v => v.DiasSinVenir)
                .ToList();

            return new AlertasCoordinacionRespuesta
            {
                BebesMasDiasSinAbrazo = bebes,
                VoluntariasSinVenir = voluntarias,
                DiasUmbralVoluntarias = diasUmbralVoluntarias
            };
        }

        /// <summary>
        /// Todo lo que pasó en un día, agrupado por categoría. Altas y bajas salen de la bitácora
        /// REGISTROACTIVIDAD, así que solo aparecen las registradas desde que existe.
        /// </summary>
        public ReporteDiarioRespuesta ObtenerReporteDiario(DateTime inicioUtc, DateTime finUtcExclusivo, DateOnly fecha)
        {
            static string NombreCompleto(string? nombre, string? apellido) => $"{nombre} {apellido}".Trim();

            var movimientos = new List<ReporteDiarioMovimiento>();

            // Abrazos iniciados en el día. El responsable es la voluntaria que abrazó.
            movimientos.AddRange(QueryAsignacionesActivas()
                .Where(a => a.idBebe != null
                            && a.fechaHoraInicio != null
                            && a.fechaHoraInicio >= inicioUtc
                            && a.fechaHoraInicio < finUtcExclusivo)
                .Select(a => new
                {
                    Inicio = a.fechaHoraInicio!.Value,
                    a.fechaHoraFin,
                    a.cierreAutomatico,
                    BebeNombre = a.bebe!.nombre,
                    BebeApellido = a.bebe.apellido,
                    VolNombre = a.voluntaria!.Nombre,
                    VolApellido = a.voluntaria.Apellido
                })
                .ToList()
                .Select(a => new ReporteDiarioMovimiento
                {
                    FechaHora = a.Inicio,
                    FechaHoraFin = a.fechaHoraFin,
                    Categoria = CategoriasReporteDiario.Abrazos,
                    Tipo = a.fechaHoraFin == null ? "En curso" : a.cierreAutomatico ? "Cierre automático" : "Finalizado",
                    Descripcion = $"Bebé {NombreCompleto(a.BebeNombre, a.BebeApellido)} con {NombreCompleto(a.VolNombre, a.VolApellido)}",
                    Responsable = NombreCompleto(a.VolNombre, a.VolApellido)
                }));

            // Movimientos de stock. idVoluntaria es quien retiró/entregó cuando se cargó.
            movimientos.AddRange(db.MOVIMIENTOSTOCK.AsNoTracking()
                .Where(m => m.fechaMovimiento != null
                            && m.fechaMovimiento >= inicioUtc
                            && m.fechaMovimiento < finUtcExclusivo)
                .Select(m => new
                {
                    Fecha = m.fechaMovimiento!.Value,
                    m.cantidad,
                    m.esEntrada,
                    m.observacion,
                    Insumo = db.INSUMO.Where(i => i.idInsumo == m.idInsumo).Select(i => i.nombre).FirstOrDefault(),
                    Voluntaria = db.VOLUNTARIA.Where(v => v.IdVoluntaria == m.idVoluntaria)
                        .Select(v => v.Nombre + " " + v.Apellido).FirstOrDefault()
                })
                .ToList()
                .Select(m => new ReporteDiarioMovimiento
                {
                    FechaHora = m.Fecha,
                    Categoria = CategoriasReporteDiario.MovimientosInsumos,
                    Tipo = m.esEntrada is "S" or "s" ? "Entrada" : "Salida",
                    Descripcion = $"{m.Insumo ?? "Insumo"}: {m.cantidad ?? 0} unidad(es)"
                                  + (string.IsNullOrWhiteSpace(m.observacion) ? "" : $" — {m.observacion.Trim()}"),
                    Responsable = string.IsNullOrWhiteSpace(m.Voluntaria) ? null : m.Voluntaria.Trim()
                }));

            // Altas y bajas de la bitácora (los cierres automáticos ya se ven en Abrazos).
            movimientos.AddRange(db.REGISTROACTIVIDAD.AsNoTracking()
                .Where(r => r.fechaHora >= inicioUtc && r.fechaHora < finUtcExclusivo
                            && (r.tipoMovimiento == TiposMovimientoActividad.Alta
                                || r.tipoMovimiento == TiposMovimientoActividad.Baja))
                .ToList()
                .Select(r => new ReporteDiarioMovimiento
                {
                    FechaHora = r.fechaHora,
                    Categoria = r.tipoMovimiento == TiposMovimientoActividad.Baja
                        ? CategoriasReporteDiario.Bajas
                        : CategoriasReporteDiario.Altas,
                    Tipo = r.tipoRegistro,
                    Descripcion = r.descripcion,
                    Responsable = r.nombreResponsable
                }));

            // Asignaciones creadas en el día (abrazos y tareas). No se guarda quién las generó.
            movimientos.AddRange(QueryAsignacionesActivas()
                .Where(a => a.fechaHoraAsignacion >= inicioUtc && a.fechaHoraAsignacion < finUtcExclusivo)
                .Select(a => new
                {
                    a.fechaHoraAsignacion,
                    EsAbrazo = a.idBebe != null,
                    BebeNombre = a.bebe != null ? a.bebe.nombre : null,
                    BebeApellido = a.bebe != null ? a.bebe.apellido : null,
                    Tarea = a.tarea != null ? a.tarea.nombre : null,
                    VolNombre = a.voluntaria!.Nombre,
                    VolApellido = a.voluntaria.Apellido
                })
                .ToList()
                .Select(a => new ReporteDiarioMovimiento
                {
                    FechaHora = a.fechaHoraAsignacion,
                    Categoria = CategoriasReporteDiario.Asignaciones,
                    Tipo = a.EsAbrazo ? "Abrazo" : "Tarea",
                    Descripcion = (a.EsAbrazo
                                      ? $"Bebé {NombreCompleto(a.BebeNombre, a.BebeApellido)}"
                                      : $"Tarea {a.Tarea ?? "sin nombre"}")
                                  + $" asignada a {NombreCompleto(a.VolNombre, a.VolApellido)}"
                }));

            // Asistencias: una fila por ingreso, con la salida si la registró.
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            movimientos.AddRange(db.ASISTENCIA.AsNoTracking()
                .Where(a => a.FechaHoraIngreso != null
                            && a.FechaHoraIngreso >= inicioUtc
                            && a.FechaHoraIngreso < finUtcExclusivo
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => new
                {
                    Ingreso = a.FechaHoraIngreso!.Value,
                    a.FechaHoraSalida,
                    VolNombre = a.Voluntaria != null ? a.Voluntaria.Nombre : null,
                    VolApellido = a.Voluntaria != null ? a.Voluntaria.Apellido : null
                })
                .ToList()
                .Select(a => new ReporteDiarioMovimiento
                {
                    FechaHora = a.Ingreso,
                    FechaHoraFin = a.FechaHoraSalida,
                    Categoria = CategoriasReporteDiario.Asistencias,
                    Tipo = a.FechaHoraSalida == null ? "Sin salida registrada" : "Con salida",
                    Descripcion = $"Asistencia de {NombreCompleto(a.VolNombre, a.VolApellido)}",
                    Responsable = NombreCompleto(a.VolNombre, a.VolApellido)
                }));

            var porCategoria = movimientos.ToLookup(m => m.Categoria);
            var categorias = CategoriasReporteDiario.Orden
                .Select(c => new ReporteDiarioCategoria
                {
                    Categoria = c,
                    Movimientos = porCategoria[c].OrderBy(m => m.FechaHora).ToList()
                })
                .ToList();
            categorias.ForEach(c => c.Cantidad = c.Movimientos.Count);

            return new ReporteDiarioRespuesta
            {
                Fecha = fecha,
                TotalMovimientos = movimientos.Count,
                Categorias = categorias
            };
        }
    }
}
