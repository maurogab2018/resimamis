using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class TAREA
    {
        [Key]

        public int idTarea { get; set; }

        public string nombre { get; set; }

        /// <summary>Vigente en el catálogo de tareas.</summary>
        // Default true: una tarea creada sin mandar "estado" quedaba inactiva y no se podía asignar.
        public bool Estado { get; set; } = true;

        /// <summary>Si es true, solo puede haber una asignación activa (sin finalizar) a la vez para esta tarea.</summary>
        public bool esUnica { get; set; }

        [JsonIgnore]
        public List<ASIGNACION>? Asignaciones { get; set; }

    }
}
