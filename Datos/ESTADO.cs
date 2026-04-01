using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class ESTADO
    {
        [Key]

        public int idEstado { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        public int idAmbito { get; set; }
        public virtual AMBITO ambito { get; set; }

        public ICollection<BEBE>? Bebes { get; set; }

        public ICollection<VOLUNTARIA>? Voluntarias { get; set; }

        public ICollection<ASIGNACION>? Asignaciones { get; set; }


    }
}
