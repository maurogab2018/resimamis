using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class INSUMO
    {
        [Key]
        public int idInsumo { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }

        public int stockMaximo { get; set; }
        public int stockMinimo { get; set; }

        public int stockActual { get; set; }

        public virtual ICollection<DETALLEASIGNACION> detalles { get; set; }
    }
}
