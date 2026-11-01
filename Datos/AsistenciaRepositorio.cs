using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    /// <remarks>
    /// Las consultas LINQ a EF no deben usar métodos estáticos (p. ej. EsAsistenciaOperativa) dentro de
    /// Where/Any: no se traducen a SQL. Filtrar bajas lógicas con (idEstado == null || idEstado != idEliminado).
    /// </remarks>
    public class AsistenciaRepositorio : IAsistenciaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;

        public AsistenciaRepositorio(ApplicationDbContext db, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
        }

        private static bool EsAsistenciaEliminada(ASISTENCIA a) =>
            a.Estado != null
            && a.Estado.ambito != null
            && a.Estado.ambito.nombre == "Asistencias"
            && a.Estado.nombre == "Eliminado";

        private static bool EsAsistenciaOperativa(ASISTENCIA a) => !EsAsistenciaEliminada(a);

        public bool registrarAsistencia(ASISTENCIA asistencia)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            // Solo bloquea si hay una jornada abierta: una vez registrada la
            // salida, la voluntaria puede volver a marcar entrada el mismo día.
            var jornadaAbierta = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.FechaHoraSalida == null
                    && a.IdVoluntaria == asistencia.IdVoluntaria
                    && (a.idEstado == null || a.idEstado != idElim));

            if (jornadaAbierta != null)
                return false;

            var idCreada = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creada", "Asistencias");
            var nuevaAsistencia = new ASISTENCIA()
            {
                IdVoluntaria = asistencia.IdVoluntaria,
                FechaHoraIngreso = asistencia.FechaHoraIngreso,
                idEstado = idCreada,
            };
            db.ASISTENCIA.Add(nuevaAsistencia);
            db.SaveChanges();
            return true;
        }

        public ASISTENCIA? consultarAsistencia(int idVoluntaria)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            // Puede haber más de una jornada en el día: interesa la última, que
            // es la que define si la voluntaria está adentro o ya salió.
            var asistenciaHoy = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == idVoluntaria
                    && (a.idEstado == null || a.idEstado != idElim))
                .OrderByDescending(a => a.FechaHoraIngreso)
                .FirstOrDefault();
            if (asistenciaHoy == null || !EsAsistenciaOperativa(asistenciaHoy))
                return null;
            return asistenciaHoy;
        }

        public bool registrarAsistenciaSalida(int idVoluntaria)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asistenciaHoy = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == idVoluntaria
                    && a.FechaHoraSalida == null
                    && (a.idEstado == null || a.idEstado != idElim));
            if (asistenciaHoy == null || !EsAsistenciaOperativa(asistenciaHoy))
                throw new ApplicationException("No existe un registro de asistencia para hoy o ya fue registrada la salida");
            asistenciaHoy.FechaHoraSalida = fechaHoy;
            db.SaveChanges();
            return true;
        }

        public List<ASISTENCIA> consultarAsistenciasFechahoy()
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var listaAsistencias = db.ASISTENCIA
                .AsSplitQuery()
                .Include(v => v.Voluntaria)
                .Include(v => v.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && (a.idEstado == null || a.idEstado != idElim))
                .OrderBy(a => a.FechaHoraIngreso)
                .ThenBy(a => a.IdVoluntaria)
                .ToList();
            return listaAsistencias;
        }

        public List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var listaAsistencias = db.ASISTENCIA
                .AsSplitQuery()
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.IdVoluntaria == idVoluntaria
                    && (a.idEstado == null || a.idEstado != idElim))
                .OrderByDescending(a => a.FechaHoraIngreso)
                .ToList();
            return listaAsistencias;
        }

        public bool eliminarAsistenciaLogico(int idAsistencia)
        {
            var row = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a => a.IdAsistencia == idAsistencia);
            if (row == null)
                throw new NotFoundException("Asistencia no encontrada con ese Id.");
            if (EsAsistenciaEliminada(row))
                return true;
            row.idEstado = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            db.SaveChanges();
            return true;
        }

        /// <summary>Todas las asistencias no eliminadas, más recientes primero.</summary>
        public List<ASISTENCIA> ListarTodasAsistencias()
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            return db.ASISTENCIA
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Voluntaria)
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a => a.idEstado == null || a.idEstado != idElim)
                .OrderByDescending(a => a.FechaHoraIngreso)
                .ToList();
        }

        /// <summary>Asistencias con ingreso en [inicioUtc, finUtcExclusivo), excluye bajas lógicas.</summary>
        public List<ASISTENCIA> ListarAsistenciasPorPeriodoUtc(DateTime inicioUtc, DateTime finUtcExclusivo)
        {
            var inicio = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
            var fin = DateTime.SpecifyKind(finUtcExclusivo, DateTimeKind.Utc);
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            return db.ASISTENCIA
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Voluntaria)
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicio
                    && a.FechaHoraIngreso < fin
                    && (a.idEstado == null || a.idEstado != idElim))
                .OrderBy(a => a.FechaHoraIngreso)
                .ThenBy(a => a.IdVoluntaria)
                .ToList();
        }

        /// <summary>
        /// Días del período en que cada voluntaria registró asistencia o tuvo asignaciones, con su
        /// turno. El turno sale del horario cargado de la voluntaria para ese día de la semana y,
        /// si no tiene, de la franja de HORARIO en la que cae su hora de ingreso.
        /// </summary>
        public List<DiaTurnoVoluntariaItem> ListarDiasTurnosVoluntarias(DateTime inicioUtc, DateTime finUtcExclusivo, int? idVoluntaria)
        {
            var idElimAsist = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var idElimAsig = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

            var asistencias = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.FechaHoraIngreso != null
                            && a.FechaHoraIngreso >= inicioUtc
                            && a.FechaHoraIngreso < finUtcExclusivo
                            && a.IdVoluntaria != null
                            && (idVoluntaria == null || a.IdVoluntaria == idVoluntaria)
                            && (a.idEstado == null || a.idEstado != idElimAsist))
                .Select(a => new { IdVoluntaria = a.IdVoluntaria!.Value, Ingreso = a.FechaHoraIngreso!.Value, a.FechaHoraSalida })
                .ToList();

            var asignaciones = db.ASIGNACION.AsNoTracking()
                .Where(a => a.fechaHoraAsignacion >= inicioUtc
                            && a.fechaHoraAsignacion < finUtcExclusivo
                            && (idVoluntaria == null || a.idVoluntaria == idVoluntaria)
                            && a.idEstado != idElimAsig)
                .Select(a => new { a.idVoluntaria, a.fechaHoraAsignacion })
                .ToList();

            // Todo se agrupa por (voluntaria, día calendario AR) una sola vez.
            var asistenciasPorDia = asistencias
                .ToLookup(a => (a.IdVoluntaria, Fecha: NegConversorFecha.FechaCalendarioArgentina(a.Ingreso)));
            var asignacionesPorDia = asignaciones
                .ToLookup(a => (IdVoluntaria: a.idVoluntaria, Fecha: NegConversorFecha.FechaCalendarioArgentina(a.fechaHoraAsignacion)));
            var dias = asistenciasPorDia.Select(g => g.Key)
                .Union(asignacionesPorDia.Select(g => g.Key))
                .ToList();
            if (dias.Count == 0)
                return new List<DiaTurnoVoluntariaItem>();

            var idsVoluntarias = dias.Select(d => d.IdVoluntaria).Distinct().ToList();
            var voluntarias = db.VOLUNTARIA.AsNoTracking()
                .Where(v => idsVoluntarias.Contains(v.IdVoluntaria))
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido })
                .ToDictionary(v => v.IdVoluntaria);
            var horarios = db.HORARIO.AsNoTracking().ToList();
            var turnosCargados = db.VOLUNTARIAHORARIO.AsNoTracking()
                .Where(vh => vh.Activa && idsVoluntarias.Contains(vh.IdVoluntaria))
                .Join(db.HORARIO, vh => vh.IdHorario, h => h.IdHorario, (vh, h) => new { vh.IdVoluntaria, h.IdDia, h.Turno })
                .ToList()
                .GroupBy(x => (x.IdVoluntaria, x.IdDia))
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Turno).Distinct()));
            var nombresDias = db.DIA.AsNoTracking().ToDictionary(d => d.IdDia, d => d.Descripcion);
            var culturaAr = new System.Globalization.CultureInfo("es-AR");

            var items = new List<DiaTurnoVoluntariaItem>(dias.Count);
            foreach (var clave in dias)
            {
                var (idVol, fecha) = clave;
                var idDia = IdDiaSemana(fecha);
                var asistenciasDia = asistenciasPorDia[clave].ToList();
                DateTime? ingreso = asistenciasDia.Count > 0 ? asistenciasDia.Min(a => a.Ingreso) : null;
                DateTime? salida = asistenciasDia.Max(a => a.FechaHoraSalida);

                string? turno;
                string origenTurno;
                if (turnosCargados.TryGetValue((idVol, idDia), out var turnoCargado))
                {
                    turno = turnoCargado;
                    origenTurno = OrigenTurnoHorario;
                }
                else
                {
                    turno = ingreso.HasValue ? TurnoPorHoraIngreso(horarios, idDia, ingreso.Value) : null;
                    origenTurno = turno == null ? OrigenTurnoSinDatos : OrigenTurnoHoraIngreso;
                }

                voluntarias.TryGetValue(idVol, out var voluntaria);
                items.Add(new DiaTurnoVoluntariaItem
                {
                    IdVoluntaria = idVol,
                    Nombre = voluntaria?.Nombre ?? "",
                    Apellido = voluntaria?.Apellido ?? "",
                    Fecha = fecha,
                    Dia = nombresDias.TryGetValue(idDia, out var nombreDia)
                        ? nombreDia
                        : culturaAr.TextInfo.ToTitleCase(culturaAr.DateTimeFormat.GetDayName(fecha.DayOfWeek)),
                    Turno = turno ?? "Sin turno asignado",
                    OrigenTurno = origenTurno,
                    FechaHoraIngreso = ingreso,
                    FechaHoraSalida = salida,
                    TieneAsistencia = asistenciasDia.Count > 0,
                    CantidadAsignaciones = asignacionesPorDia[clave].Count()
                });
            }

            return items
                .OrderBy(i => i.Fecha)
                .ThenBy(i => i.Nombre)
                .ThenBy(i => i.Apellido)
                .ToList();
        }

        public const string OrigenTurnoHorario = "Horario de la voluntaria";
        public const string OrigenTurnoHoraIngreso = "Según hora de ingreso";
        public const string OrigenTurnoSinDatos = "Sin datos";

        /// <summary>Id de la tabla DIA: 1 = Lunes ... 7 = Domingo.</summary>
        private static int IdDiaSemana(DateOnly fecha) =>
            fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

        /// <summary>
        /// Franja de HORARIO de ese día que contiene la hora de ingreso (hora Argentina). Si entra
        /// en más de una gana la más corta, para que "Mañana" le gane a "Jornada completa".
        /// </summary>
        private static string? TurnoPorHoraIngreso(List<HORARIO> horarios, int idDia, DateTime ingresoUtc)
        {
            var hora = NegConversorFecha.HoraArgentina(ingresoUtc).TimeOfDay;
            return horarios
                .Where(h => h.IdDia == idDia && h.HoraIngreso <= hora && hora < h.HoraSalida)
                .OrderBy(h => h.HoraSalida - h.HoraIngreso)
                .Select(h => h.Turno)
                .FirstOrDefault();
        }
    }
}
