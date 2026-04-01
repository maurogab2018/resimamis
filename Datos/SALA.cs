using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class SALA
    {
        [Key]
        public int IdSala { get; set; }

        public string Nombre { get; set; }
    }
}
