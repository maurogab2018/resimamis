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
                            && a.fechaHoraFin != null);

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

        public EstadisticaVisitasRespuesta ObtenerEstadisticasVisitas(DateTime fechaInicio, DateTime fechaFin)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var visitas = db.VISITA.AsNoTracking()
                .Where(v => v.Activa && v.fechaHoraVisita >= inicioUtc && v.fechaHoraVisita < finUtc)
                .Select(v => new { v.idBebe, v.familiar, v.fechaHoraVisita })
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
                TotalVisitas = visitas.Count,
                BebesVisitados = visitas.Select(v => v.idBebe).Distinct().Count(),
                PorDia = porDia,
                PorFamiliar = porFamiliar
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
                if (a.fechaHoraInicio.HasValue && a.fechaHoraFin.HasValue)
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
                    Comentario = a.comentario
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
                AbrazosFinalizados = abrazos.Count(a => a.FechaHoraInicio.HasValue && a.FechaHoraFin.HasValue),
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
                if (a.fechaHoraInicio.HasValue && a.fechaHoraFin.HasValue)
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
                    Comentario = a.comentario
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
                AbrazosFinalizados = abrazos.Count(a => a.FechaHoraInicio.HasValue && a.FechaHoraFin.HasValue),
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
                            && a.fechaHoraFin != null
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
                            && a.fechaHoraFin != null
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
                            && a.fechaHoraFin != null
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
    }
}
