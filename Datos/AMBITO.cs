using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class AMBITO
    {
        [Key]

        public int idAmbito { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        public ICollection<ESTADO>? estados { get; set; }
    }
}
