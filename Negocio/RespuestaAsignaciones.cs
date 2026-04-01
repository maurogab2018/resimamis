using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    public class RespuestaAsignaciones
    {
        public int idAsignacion { get; set; }

        public int? idTarea { get; set; }

        public int? idBebe { get; set; }

        public string? nombreBebe { get; set; }


        public int idVoluntaria{ get; set; }

        public string nombreVoluntaria { get; set; }

        public DateTime? fechaHoraInicio { get; set; }

        public DateTime? fechaHoraFin { get; set; }
        public DateTime fechaHoraAsignacion { get; set; }

        public string estadoAsignacion { get; set; }

        public int? sala { get; set; }
        public List<DetalleAsignacionResumido>? detalles { get; set; }


    }
}
