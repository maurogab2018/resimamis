using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    /// <remarks>
    /// Las consultas LINQ a EF no deben usar métodos estáticos (p. ej. EsAsistenciaOperativa) dentro de
    /// Where/Any: no se traducen a SQL. Filtrar bajas lógicas con (idEstado == null || idEstado != idEliminado).
    /// </remarks>
    public class AsistenciaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly EstadoRepositorio estadoRepositorio;

        public AsistenciaRepositorio()
        {
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
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
            var yaExisteAsistencia = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == asistencia.IdVoluntaria
                    && (a.idEstado == null || a.idEstado != idElim));

            if (yaExisteAsistencia != null)
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

        public bool consultarAsistencia(int idVoluntaria)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asistenciaHoy = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == idVoluntaria
                    && (a.idEstado == null || a.idEstado != idElim));
            return asistenciaHoy != null && EsAsistenciaOperativa(asistenciaHoy);
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
                throw new Exception("No existe un registro de asistencia para hoy o ya fue registrado");
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
            if (listaAsistencias.Count == 0)
                throw new Exception("No existes asistencias para la fecha");
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
            if (listaAsistencias.Count == 0)
                throw new Exception("No existes asistencias para esa voluntaria");
            return listaAsistencias;
        }

        public bool eliminarAsistenciaLogico(int idAsistencia)
        {
            var row = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a => a.IdAsistencia == idAsistencia);
            if (row == null)
                throw new ApplicationException("Asistencia no existente con ese Id");
            if (EsAsistenciaEliminada(row))
                return true;
            row.idEstado = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            db.SaveChanges();
            return true;
        }

        /// <summary>Asistencias con ingreso en [inicioUtc, finUtcExclusivo), excluye bajas lógicas.</summary>
        public List<ASISTENCIA> ListarAsistenciasPorPeriodoUtc(DateTime inicioUtc, DateTime finUtcExclusivo)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asistencias");
            return db.ASISTENCIA
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Voluntaria)
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioUtc
                    && a.FechaHoraIngreso < finUtcExclusivo
                    && (a.idEstado == null || a.idEstado != idElim))
                .OrderBy(a => a.FechaHoraIngreso)
                .ThenBy(a => a.IdVoluntaria)
                .ToList();
        }
    }
}
