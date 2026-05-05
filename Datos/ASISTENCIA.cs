using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class ASISTENCIA
    {
        [Key]
        public int? IdAsistencia { get; set; }

        public int? IdVoluntaria{ get; set; }
    
        public int? IdHorario { get; set; }
        
        public DateTime? FechaHoraSalida { get; set; }

        public DateTime? FechaHoraIngreso { get; set; }

        /// <summary>Estado de la asistencia (ámbito Asistencias en ESTADO).</summary>
        public int? idEstado { get; set; }

        [JsonIgnore]
        public virtual ESTADO? Estado { get; set; }

        public virtual VOLUNTARIA? Voluntaria { get; set; }
    }
}
