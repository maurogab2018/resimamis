using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
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
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var yaExisteAsistencia = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == asistencia.IdVoluntaria);

            if (yaExisteAsistencia != null && EsAsistenciaOperativa(yaExisteAsistencia))
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
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asistenciaHoy = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && a.IdVoluntaria == idVoluntaria);
            return asistenciaHoy != null && EsAsistenciaOperativa(asistenciaHoy);
        }

        public bool registrarAsistenciaSalida(int idVoluntaria)
        {
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
                    && a.FechaHoraSalida == null);
            if (asistenciaHoy == null || !EsAsistenciaOperativa(asistenciaHoy))
                throw new Exception("No existe un registro de asistencia para hoy o ya fue registrado");
            asistenciaHoy.FechaHoraSalida = fechaHoy;
            db.SaveChanges();
            return true;
        }

        public List<ASISTENCIA> consultarAsistenciasFechahoy()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var listaAsistencias = db.ASISTENCIA
                .Include(v => v.Voluntaria)
                .Include(v => v.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a =>
                    a.FechaHoraIngreso != null
                    && a.FechaHoraIngreso >= inicioDia
                    && a.FechaHoraIngreso < finDia
                    && EsAsistenciaOperativa(a))
                .Select(v => new ASISTENCIA()
                {
                    IdAsistencia = v.IdAsistencia,
                    FechaHoraIngreso = v.FechaHoraIngreso,
                    FechaHoraSalida = v.FechaHoraSalida != null ? v.FechaHoraSalida : v.FechaHoraSalida,
                    IdVoluntaria = v.IdVoluntaria,
                    IdHorario = v.IdHorario,
                    idEstado = v.idEstado,
                    Voluntaria = v.Voluntaria,
                }).ToList();
            if (listaAsistencias.Count == 0)
                throw new Exception("No existes asistencias para la fecha");
            return listaAsistencias;
        }

        public List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria)
        {
            var listaAsistencias = db.ASISTENCIA
                .Include(a => a.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(a => a.IdVoluntaria == idVoluntaria && EsAsistenciaOperativa(a))
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
                    && !(a.Estado != null
                        && a.Estado.ambito != null
                        && a.Estado.ambito.nombre == "Asistencias"
                        && a.Estado.nombre == "Eliminado"))
                .OrderBy(a => a.FechaHoraIngreso)
                .ThenBy(a => a.IdVoluntaria)
                .ToList();
        }
    }
}
