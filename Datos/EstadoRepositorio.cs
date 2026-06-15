using Microsoft.EntityFrameworkCore;

namespace ResimamisBackend.Datos
{
    public class EstadoRepositorio
    {
        private readonly ApplicationDbContext db;

        public EstadoRepositorio()
        {
            db = new ApplicationDbContext();
        }

        /// <summary>Resuelve el id de ESTADO con nombre "Eliminado" para el ámbito indicado.</summary>
        public int ObtenerIdEstadoEliminado(string nombreAmbito)
        {
            var row = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .FirstOrDefault(e => e.nombre == "Eliminado" && e.ambito.nombre == nombreAmbito);
            if (row == null)
                throw new ApplicationException(
                    $"No existe el estado 'Eliminado' para el ámbito '{nombreAmbito}'. Ejecute las migraciones o inserte el registro en ESTADO.");
            return row.idEstado;
        }

        /// <summary>Ej. nombreEstado "Creada", nombreAmbito "Asistencias".</summary>
        public int ObtenerIdEstadoPorNombreYAmbito(string nombreEstado, string nombreAmbito)
        {
            var row = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .FirstOrDefault(e => e.nombre == nombreEstado && e.ambito.nombre == nombreAmbito);
            if (row == null)
                throw new ApplicationException(
                    $"No existe el estado '{nombreEstado}' para el ámbito '{nombreAmbito}'. Ejecute las migraciones o inserte el registro en ESTADO.");
            return row.idEstado;
        }

        /// <summary>Prueba varios nombres en orden (útil cuando prod y seed usan nombres distintos).</summary>
        public int ObtenerIdEstadoPorNombresYAmbito(string nombreAmbito, params string[] nombresEstado)
        {
            if (nombresEstado == null || nombresEstado.Length == 0)
                throw new ApplicationException($"Debe indicar al menos un nombre de estado para el ámbito '{nombreAmbito}'.");

            foreach (var nombre in nombresEstado)
            {
                var row = db.ESTADO
                    .AsNoTracking()
                    .Include(e => e.ambito)
                    .FirstOrDefault(e => e.nombre == nombre && e.ambito.nombre == nombreAmbito);
                if (row != null)
                    return row.idEstado;
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
    }
}
