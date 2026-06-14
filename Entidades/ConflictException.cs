namespace ResimamisBackend.Entidades;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
