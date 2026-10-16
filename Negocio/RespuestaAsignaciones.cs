using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// DTO de respuesta de asignaciones, y también el cuerpo del PUT.
    /// Los campos que el PUT puede limpiar llevan un flag `*Presente`: System.Text.Json
    /// sólo llama al setter si la propiedad vino en el JSON, así que el flag distingue
    /// "no lo mandó" (conservar) de "lo mandó en null" (borrar). Sin esto no hay forma
    /// de vaciar un comentario ni una fecha.
    /// </summary>
    public class RespuestaAsignaciones
    {
        public int idAsignacion { get; set; }

        private int? _idTarea;
        public int? idTarea
        {
            get => _idTarea;
            set { _idTarea = value; idTareaPresente = true; }
        }

        private int? _idBebe;
        public int? idBebe
        {
            get => _idBebe;
            set { _idBebe = value; idBebePresente = true; }
        }

        public string? nombreBebe { get; set; }

        public string? nombreTarea { get; set; }


        public int idVoluntaria{ get; set; }

        public string nombreVoluntaria { get; set; }

        private DateTime? _fechaHoraInicio;
        public DateTime? fechaHoraInicio
        {
            get => _fechaHoraInicio;
            set { _fechaHoraInicio = value; fechaHoraInicioPresente = true; }
        }

        private DateTime? _fechaHoraFin;
        public DateTime? fechaHoraFin
        {
            get => _fechaHoraFin;
            set { _fechaHoraFin = value; fechaHoraFinPresente = true; }
        }

        public DateTime fechaHoraAsignacion { get; set; }

        public string estadoAsignacion { get; set; }

        private string? _comentario;
        public string? comentario
        {
            get => _comentario;
            set { _comentario = value; comentarioPresente = true; }
        }

        public int? sala { get; set; }
        public string? nombreSala { get; set; }
        public List<DetalleAsignacionResumido>? detalles { get; set; }

        [JsonIgnore] public bool idTareaPresente { get; private set; }
        [JsonIgnore] public bool idBebePresente { get; private set; }
        [JsonIgnore] public bool fechaHoraInicioPresente { get; private set; }
        [JsonIgnore] public bool fechaHoraFinPresente { get; private set; }
        [JsonIgnore] public bool comentarioPresente { get; private set; }
    }
}
