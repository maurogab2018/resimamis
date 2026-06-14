using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class TareaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly EstadoRepositorio estadoRepositorio;

        public TareaRepositorio()
        {
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
        }

        private int IdEstadoEliminadoAsignaciones() =>
            estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

        public List<TAREA> listarTareas()
        {
            return db.TAREA
                .AsNoTracking()
                .OrderBy(t => t.nombre)
                .ToList();
        }

        public List<TAREA> listarTareasActivas()
        {
            return db.TAREA
                .AsNoTracking()
                .Where(t => t.Estado)
                .OrderBy(t => t.nombre)
                .ToList();
        }

        /// <summary>
        /// Tareas activas disponibles para asignar hoy.
        /// Las únicas (<see cref="TAREA.esUnica"/>) se excluyen si ya tienen una asignación activa sin finalizar.
        /// </summary>
        public List<TAREA> listarTareasDisponiblesParaAsignar()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idElim = IdEstadoEliminadoAsignaciones();

            var idsTareasUnicasOcupadas = db.ASIGNACION
                .AsNoTracking()
                .Where(a =>
                    a.idTarea != null
                    && a.fechaHoraAsignacion >= inicioDia
                    && a.fechaHoraAsignacion < finDia
                    && a.idEstado != idElim
                    && a.fechaHoraFin == null)
                .Join(
                    db.TAREA.AsNoTracking().Where(t => t.esUnica),
                    a => a.idTarea,
                    t => t.idTarea,
                    (a, t) => t.idTarea)
                .Distinct()
                .ToList();

            return db.TAREA
                .AsNoTracking()
                .Where(t => t.Estado && (!t.esUnica || !idsTareasUnicasOcupadas.Contains(t.idTarea)))
                .OrderBy(t => t.nombre)
                .ToList();
        }

        public bool tareaUnicaOcupadaHoy(int idTarea)
        {
            var tarea = db.TAREA.AsNoTracking().FirstOrDefault(t => t.idTarea == idTarea);
            if (tarea == null || !tarea.esUnica)
                return false;

            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idElim = IdEstadoEliminadoAsignaciones();

            return db.ASIGNACION.Any(a =>
                a.idTarea == idTarea
                && a.fechaHoraAsignacion >= inicioDia
                && a.fechaHoraAsignacion < finDia
                && a.idEstado != idElim
                && a.fechaHoraFin == null);
        }

        public TAREA? obtenerPorId(int idTarea)
        {
            return db.TAREA.AsNoTracking().FirstOrDefault(t => t.idTarea == idTarea);
        }

        public TAREA obtenerParaModificar(int idTarea)
        {
            var tarea = db.TAREA.FirstOrDefault(t => t.idTarea == idTarea);
            if (tarea == null)
                throw new NotFoundException("Tarea no encontrada con ese id.");
            return tarea;
        }

        public bool registrarTarea(TAREA tarea)
        {
            db.TAREA.Add(tarea);
            db.SaveChanges();
            return true;
        }

        public bool modificarTarea(TAREA datos, TAREA existente)
        {
            existente.nombre = datos.nombre;
            existente.Estado = datos.Estado;
            existente.esUnica = datos.esUnica;
            db.SaveChanges();
            return true;
        }

        public bool eliminarTareaLogica(int idTarea)
        {
            var tarea = obtenerParaModificar(idTarea);
            if (!tarea.Estado)
                return true;
            tarea.Estado = false;
            db.SaveChanges();
            return true;
        }

        public bool existeOtraTareaConNombre(string nombre, int? exceptIdTarea = null)
        {
            var normalizado = nombre.Trim().ToLower();
            return db.TAREA.Any(t =>
                t.nombre.ToLower() == normalizado
                && (exceptIdTarea == null || t.idTarea != exceptIdTarea));
        }
    }
}
