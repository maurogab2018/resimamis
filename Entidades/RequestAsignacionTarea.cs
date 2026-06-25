using ResimamisBackend.Datos;

namespace ResimamisBackend.Entidades
{
    public class RequestAsignacionTarea
    {
        public int idVoluntaria { get; set; }

        /// <summary>
        /// En <c>generarTarea</c> es id de bebé (BEBE.ID).
        /// En <c>generarTareaCatalogo</c> es id de la tabla TAREA.
        /// </summary>
        public int idTarea { get; set; }
    }

    public class RequestAsignacionTareas
    {
        public List<int> idVoluntarias { get; set; } = new();

        /// <summary>
        /// En <c>generarTareas</c> son ids de bebé (BEBE.ID).
        /// En <c>generarTareasPorId</c> son ids de la tabla TAREA.
        /// </summary>
        public List<int> idTareas { get; set; } = new();
    }

    public class VoluntariaConAsignaciones
    {
        public VOLUNTARIA  Voluntaria { get; set; }
        public int CantidadAsignacionesHoy { get; set; }
    }
}
