namespace ResimamisBackend.Entidades;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
