using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Parámetros del sistema editables desde la base, sin redeploy (ej. Asistente.MaxPreguntasDia).
    /// Si una clave no está o su valor no es válido, se usa el valor por defecto del código.
    /// </summary>
    public class PARAMETRO
    {
        [Key]
        [MaxLength(100)]
        public string clave { get; set; } = "";

        [MaxLength(500)]
        public string valor { get; set; } = "";

        [MaxLength(300)]
        public string? descripcion { get; set; }
    }
}
