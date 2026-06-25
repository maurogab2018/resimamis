namespace ResimamisBackend.Negocio
{
    public class HorarioVoluntaria
    {
        public int IdDia { get; set; }

        public int IdVoluntaria { get; set; }

        /// <summary>Opcional: si viene del front sin idDia/turno, se resuelve contra HORARIO.</summary>
        public int IdHorario { get; set; }

        public string Turno { get; set; } = string.Empty;
    }
}
