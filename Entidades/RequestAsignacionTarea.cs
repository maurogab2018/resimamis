using ResimamisBackend.Datos;

namespace ResimamisBackend.Entidades
{
    public class RequestAsignacionTarea
    {
        public int idVoluntaria { get; set; }

        public int idTarea { get; set; }


    }

    public class RequestAsignacionTareas
    {
        public List<int> idVoluntarias { get; set; }

        public List<int> idTareas { get; set; }


    }

    public class VoluntariaConAsignaciones
    {
        public VOLUNTARIA  Voluntaria { get; set; }
        public int CantidadAsignacionesHoy { get; set; }
    }
}
