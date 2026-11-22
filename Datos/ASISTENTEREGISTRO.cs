using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Registro de cada pregunta al asistente: qué se preguntó, qué contestó, cómo le fue y la
    /// calificación opcional de la coordinadora (👍/👎). Sirve para encontrar respuestas malas y
    /// corregirlas. Tiene tope: se conservan las 1000 más nuevas y, aparte, las 200 👎 más nuevas.
    /// </summary>
    public class ASISTENTEREGISTRO
    {
        [Key]
        public int idAsistenteRegistro { get; set; }

        /// <summary>UTC.</summary>
        public DateTime fecha { get; set; }

        public int dni { get; set; }

        [MaxLength(2000)]
        public string pregunta { get; set; } = "";

        [MaxLength(4000)]
        public string respuesta { get; set; } = "";

        /// <summary>Herramientas usadas, separadas por coma.</summary>
        [MaxLength(500)]
        public string herramientas { get; set; } = "";

        /// <summary>ok · insistio (se rindió o inventó números y se le exigió consultar) · respaldo (contestó el plan B).</summary>
        [MaxLength(20)]
        public string resultado { get; set; } = "ok";

        public int duracionMs { get; set; }

        public int tokens { get; set; }

        /// <summary>1 = bien, -1 = mal, null = sin calificar (votar es opcional).</summary>
        public short? calificacion { get; set; }

        [MaxLength(300)]
        public string? comentario { get; set; }
    }
}
