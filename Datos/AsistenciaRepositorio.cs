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
    }
}
