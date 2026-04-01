using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class HORARIO
    {
        [Key]
        public int IdHorario { get; set; }
        public int IdDia { get; set; }

        public string Turno { get; set; }

        public TimeSpan HoraIngreso { get; set; }
        public TimeSpan HoraSalida { get; set; }

    }
}
