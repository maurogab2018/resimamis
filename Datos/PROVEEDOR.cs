using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class PROVEEDOR
    {
        [Key]
        public int idProveedor { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
    }
}
