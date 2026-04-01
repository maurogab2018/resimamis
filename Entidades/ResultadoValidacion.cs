namespace ResimamisBackend.Entidades
{
    public class ResultadoValidacion
    {
        public bool Exito => !Errores.Any();
        public List<string> Errores { get; set; } = new();
    }
}
