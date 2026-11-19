using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Uso del asistente por usuaria y día (calendario Argentina): cuántas preguntas hizo y cuántos
    /// tokens se gastaron en OpenAI. Sirve para el tope diario y para saber cuánto cuesta.
    /// </summary>
    public class ASISTENTEUSO
    {
        [Key]
        public int idAsistenteUso { get; set; }

        public int dni { get; set; }

        /// <summary>Día en Argentina, sin hora.</summary>
        public DateOnly fecha { get; set; }

        public int preguntas { get; set; }

        public int tokensEntrada { get; set; }

        public int tokensSalida { get; set; }
    }
}
