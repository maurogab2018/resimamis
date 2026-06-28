using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegTareas : INegTareas
    {
        private readonly ITareaRepositorio tareaRepositorio;

        public NegTareas(ITareaRepositorio tareaRepositorio)
        {
            this.tareaRepositorio = tareaRepositorio;
        }

        private static void ValidarTarea(TAREA tarea)
        {
            if (tarea == null)
                throw new ApplicationException("Tarea inválida.");
            if (string.IsNullOrWhiteSpace(tarea.nombre))
                throw new ApplicationException("El nombre es obligatorio.");
            if (tarea.nombre.Trim().Length > 100)
                throw new ApplicationException("El nombre no permite más de 100 caracteres.");
        }

        public List<TAREA> listarTareas()
        {
            return tareaRepositorio.listarTareas();
        }

        public List<TAREA> listarTareasDisponiblesParaAsignar()
        {
            return tareaRepositorio.listarTareasDisponiblesParaAsignar();
        }

        public TAREA? consultarTarea(int idTarea)
        {
            var tarea = tareaRepositorio.obtenerPorId(idTarea);
            if (tarea == null)
                throw new NotFoundException("Tarea inexistente con ese id.");
            return tarea;
        }

        public bool registrarTarea(TAREA tarea)
        {
            ValidarTarea(tarea);
            tarea.nombre = tarea.nombre.Trim();
            if (tareaRepositorio.existeOtraTareaConNombre(tarea.nombre))
                throw new ConflictException("Ya existe una tarea con ese nombre.");
            return tareaRepositorio.registrarTarea(tarea);
        }

        public bool modificarTarea(int idTarea, TAREA tarea)
        {
            ValidarTarea(tarea);
            tarea.nombre = tarea.nombre.Trim();
            var existente = tareaRepositorio.obtenerParaModificar(idTarea);
            if (tareaRepositorio.existeOtraTareaConNombre(tarea.nombre, idTarea))
                throw new ConflictException("Ya existe otra tarea con ese nombre.");
            return tareaRepositorio.modificarTarea(tarea, existente);
        }

        public bool eliminarTarea(int idTarea)
        {
            if (tareaRepositorio.tareaUnicaOcupadaHoy(idTarea))
                throw new ApplicationException("No se puede dar de baja: la tarea tiene una asignación activa sin finalizar.");
            return tareaRepositorio.eliminarTareaLogica(idTarea);
        }

        public void ValidarTareaDisponibleParaAsignar(int idTarea)
        {
            var tarea = tareaRepositorio.obtenerPorId(idTarea);
            if (tarea == null)
                throw new NotFoundException("Tarea no encontrada.");
            if (!tarea.Estado)
                throw new ApplicationException("La tarea no está activa.");
            if (tarea.esUnica && tareaRepositorio.tareaUnicaOcupadaHoy(idTarea))
                throw new ConflictException($"La tarea '{tarea.nombre}' ya está asignada y aún no finalizó.");
        }
    }
}
