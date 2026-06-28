namespace ResimamisBackend.Negocio.Interfaces;

public interface INegEnvioMail
{
    /// <summary>Envía un correo HTML. Devuelve mensaje de éxito o error.</summary>
    string EnviarMail(string to, string asunto, string bodyHtml);

    /// <summary>Envía el mismo correo a varios destinatarios.</summary>
    string EnviarMail(IEnumerable<string> destinatarios, string asunto, string bodyHtml);

    bool EstaConfigurado();
}
