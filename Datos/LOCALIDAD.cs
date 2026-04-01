using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class LOCALIDAD
    {
        [Key]

        public int idLocalidad { get; set; }
        
        public string nombre { get; set; }

        public int idProvincia { get; set; }
    }
}
