using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class VOLUNTARIAHORARIO
    {
       [Key]
        public int IdHorarioVoluntaria { get; set; }
        public int IdHorario { get; set; }
        public int IdVoluntaria { get; set; }
    }
}
