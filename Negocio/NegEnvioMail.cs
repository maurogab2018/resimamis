using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegEnvioMail : INegEnvioMail
    {
        private readonly IConfiguration configuration;

        public NegEnvioMail(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool EstaConfigurado()
        {
            var enabled = configuration.GetValue("Email:Enabled", false);
            var host = ResolverSmtpHost();
            var from = configuration["Email:From"];
            var password = ResolverSmtpPassword();
            var user = ResolverSmtpUser();
            return enabled
                && !string.IsNullOrWhiteSpace(host)
                && !string.IsNullOrWhiteSpace(from)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(password);
        }

        public string EnviarMail(string to, string asunto, string bodyHtml) =>
            EnviarMail(new[] { to }, asunto, bodyHtml);

        public string EnviarMail(IEnumerable<string> destinatarios, string asunto, string bodyHtml)
        {
            var lista = destinatarios
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (lista.Count == 0)
                return "No hay destinatarios válidos.";

            if (!EstaConfigurado())
                return "Envío de correo deshabilitado o incompleto. Configure Email:Enabled, From y Smtp en appsettings o variables de entorno.";

            var from = configuration["Email:From"]!;
            var displayName = configuration["Email:DisplayName"] ?? "Resimamis";
            var host = ResolverSmtpHost()!;
            var port = configuration.GetValue("Email:Smtp:Port", 587);
            var user = ResolverSmtpUser()!;
            var password = ResolverSmtpPassword()!;
            var enableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true);

            try
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(from, displayName),
                    Subject = asunto,
                    Body = bodyHtml,
                    IsBodyHtml = true
                };
                foreach (var to in lista)
                    mail.To.Add(to);

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, password),
                    EnableSsl = enableSsl
                };
                client.Send(mail);
                return "Correo enviado correctamente.";
            }
            catch (Exception ex)
            {
                return $"Error al enviar correo: {ex.Message}";
            }
        }

        private string? ResolverSmtpHost()
        {
            var host = configuration["Email:Smtp:Host"];
            return string.IsNullOrWhiteSpace(host) ? null : host.Trim();
        }

        private string? ResolverSmtpUser() =>
            configuration["Email:Smtp:User"]
            ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_USER");

        private string? ResolverSmtpPassword() =>
            configuration["Email:Smtp:Password"]
            ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD");
    }
}
