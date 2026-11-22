using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;

namespace ResimamisBackend.Datos
{
    public class EstadoRepositorio : IEstadoRepositorio
    {
        private readonly ApplicationDbContext db;

        public EstadoRepositorio(ApplicationDbContext db)
        {
            this.db = db;
        }

        /// <summary>
        /// Ids ya resueltos en este pedido (el repositorio es scoped). Los estados no cambian durante un
        /// request y antes se consultaban cientos de veces por cada pregunta del asistente.
        /// </summary>
        private readonly Dictionary<(string Nombre, string Ambito), int> cache = new();

        private int? Buscar(string nombreEstado, string nombreAmbito)
        {
            if (cache.TryGetValue((nombreEstado, nombreAmbito), out var guardado))
                return guardado;
            var row = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .FirstOrDefault(e => e.nombre == nombreEstado && e.ambito.nombre == nombreAmbito);
            if (row == null)
                return null;
            cache[(nombreEstado, nombreAmbito)] = row.idEstado;
            return row.idEstado;
        }

        /// <summary>Resuelve el id de ESTADO con nombre "Eliminado" para el ámbito indicado.</summary>
        public int ObtenerIdEstadoEliminado(string nombreAmbito)
        {
            return Buscar("Eliminado", nombreAmbito)
                ?? throw new ApplicationException(
                    $"No existe el estado 'Eliminado' para el ámbito '{nombreAmbito}'. Ejecute las migraciones o inserte el registro en ESTADO.");
        }

        /// <summary>Ej. nombreEstado "Creada", nombreAmbito "Asistencias".</summary>
        public int ObtenerIdEstadoPorNombreYAmbito(string nombreEstado, string nombreAmbito)
        {
            return Buscar(nombreEstado, nombreAmbito)
                ?? throw new ApplicationException(
                    $"No existe el estado '{nombreEstado}' para el ámbito '{nombreAmbito}'. Ejecute las migraciones o inserte el registro en ESTADO.");
        }

        /// <summary>Prueba varios nombres en orden (útil cuando prod y seed usan nombres distintos).</summary>
        public int ObtenerIdEstadoPorNombresYAmbito(string nombreAmbito, params string[] nombresEstado)
        {
            if (nombresEstado == null || nombresEstado.Length == 0)
                throw new ApplicationException($"Debe indicar al menos un nombre de estado para el ámbito '{nombreAmbito}'.");

            foreach (var nombre in nombresEstado)
            {
                var id = Buscar(nombre, nombreAmbito);
                if (id != null)
                    return id.Value;
            }

            throw new ApplicationException(
                $"No existe ninguno de los estados [{string.Join(", ", nombresEstado)}] para el ámbito '{nombreAmbito}'.");
        }

        /// <summary>Voluntaria libre tras finalizar abrazo o tarea.</summary>
        public int ObtenerIdVoluntariaDisponible() =>
            ObtenerIdEstadoPorNombresYAmbito("Voluntarias", "Disponible", "Activa");

        /// <summary>Voluntaria en abrazo activo.</summary>
        public int ObtenerIdVoluntariaAbrazando() =>
            ObtenerIdEstadoPorNombresYAmbito("Voluntarias", "Abrazando");

        /// <summary>Voluntaria realizando una tarea (sin bebé).</summary>
        public int ObtenerIdVoluntariaEnTarea() =>
            ObtenerIdEstadoPorNombresYAmbito("Voluntarias", "Asignada", "Ayudando");

        /// <summary>Bebé esperando abrazo.</summary>
        public int ObtenerIdBebeSinAbrazar() =>
            ObtenerIdEstadoPorNombresYAmbito("Bebes", "Sin abrazar");

        /// <summary>Bebé en abrazo activo.</summary>
        public int ObtenerIdBebeAbrazado() =>
            ObtenerIdEstadoPorNombresYAmbito("Bebes", "Abrazado", "Asignado");

        /// <summary>Asignación con abrazo en curso.</summary>
        public int ObtenerIdEstadoAsignacionIniciado() =>
            ObtenerIdEstadoPorNombreYAmbito("Iniciado", "Asignaciones");

        /// <summary>Asignación con abrazo cerrado.</summary>
        public int ObtenerIdEstadoAsignacionFinalizado() =>
            ObtenerIdEstadoPorNombreYAmbito("Finalizado", "Asignaciones");
    }
}
