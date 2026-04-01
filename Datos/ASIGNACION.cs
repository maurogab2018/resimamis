using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class ASIGNACION
    {
        [Key]
        public int idAsignacion { get; set; }

        public DateTime? fechaHoraInicio { get; set; }
                         
        public DateTime? fechaHoraFin{ get; set; }
        public DateTime fechaHoraAsignacion { get; set; }

        public int? idBebe { get; set; }

        public int? idTarea { get; set; }

        public int idEstado { get; set; }

        public int idVoluntaria { get; set; }

        public string? comentario { get; set; }

        public virtual VOLUNTARIA? voluntaria { get; set; }

        public virtual ESTADO? estado { get; set; }

        public virtual BEBE? bebe { get; set; }

        public virtual TAREA? tarea { get; set; }

        public virtual ICollection<DETALLEASIGNACION>? detallesAsignacion { get; set; }

    }
}
