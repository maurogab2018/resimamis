using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class DIA
    {
        [Key]
        public int IdDia { get; set; }
        public string Descripcion { get; set; }
    }
}
