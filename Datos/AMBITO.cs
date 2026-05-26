using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class AMBITO
    {
        [Key]

        public int idAmbito { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        [JsonIgnore]
        public ICollection<ESTADO>? estados { get; set; }
    }
}
