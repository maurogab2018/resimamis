using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class ROL
    {
        [Key]

        public int IdRol { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public virtual ICollection<VOLUNTARIA>? Voluntarias { get; set; }

    }
}
