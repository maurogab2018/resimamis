using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class SALA
    {
        [Key]
        public int IdSala { get; set; }

        public string Nombre { get; set; }

        /// <summary>Vigente en el catálogo de salas.</summary>
        public bool Activa { get; set; } = true;
    }
}
