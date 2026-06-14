namespace ResimamisBackend.Entidades;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
