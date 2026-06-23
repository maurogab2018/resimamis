namespace ResimamisBackend.Entidades
{
    public class HorarioVoluntariaRespuesta
    {
        public int IdHorarioVoluntaria { get; set; }
        public int IdHorario { get; set; }
        public int IdVoluntaria { get; set; }
        public int IdDia { get; set; }
        public string Dia { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public TimeSpan HoraIngreso { get; set; }
        public TimeSpan HoraSalida { get; set; }
    }
}
