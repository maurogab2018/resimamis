using System.ComponentModel.DataAnnotations;

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

        public virtual VOLUNTARIA? Voluntaria { get; set; }
    }
}
