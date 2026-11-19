using System.Net;
using System.Net.Http.Json;
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

        // Render gratis bloquea los puertos SMTP (25, 465, 587) desde sep-2025: con una clave de
        // Brevo el correo sale por su API HTTPS. Sin clave se sigue usando el SMTP de siempre.
        private const string BrevoUrl = "https://api.brevo.com/v3/smtp/email";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public bool EstaConfigurado()
        {
            if (!string.IsNullOrWhiteSpace(ResolverBrevoApiKey()))
                return !string.IsNullOrWhiteSpace(ResolverFrom());

            var enabled = configuration.GetValue("Email:Enabled", false);
            var host = ResolverSmtpHost();
            var from = ResolverFrom();
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
                return "Envío de correo deshabilitado o incompleto. Configure BREVO_API_KEY y EMAIL_FROM, o Email:Enabled, From y Smtp, en variables de entorno.";

            var from = ResolverFrom()!;
            var displayName = configuration["Email:DisplayName"] ?? "Resimamis";

            var brevoKey = ResolverBrevoApiKey();
            if (!string.IsNullOrWhiteSpace(brevoKey))
                return EnviarPorBrevo(brevoKey, from, displayName, lista, asunto, bodyHtml);
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

        private static string EnviarPorBrevo(string apiKey, string from, string displayName,
            List<string> destinatarios, string asunto, string bodyHtml)
        {
            try
            {
                var cuerpo = new
                {
                    sender = new { name = displayName, email = from },
                    to = destinatarios.Select(d => new { email = d }).ToArray(),
                    subject = asunto,
                    htmlContent = bodyHtml,
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, BrevoUrl)
                {
                    Content = JsonContent.Create(cuerpo),
                };
                request.Headers.Add("api-key", apiKey);
                request.Headers.Accept.ParseAdd("application/json");

                using var response = Http.Send(request);
                if (response.IsSuccessStatusCode)
                    return "Correo enviado correctamente.";

                // El detalle de Brevo explica el rechazo (remitente sin verificar, IP no autorizada...).
                var detalle = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (detalle.Length > 300)
                    detalle = detalle[..300];
                return $"Error al enviar correo: Brevo respondió {(int)response.StatusCode}. {detalle}";
            }
            catch (Exception ex)
            {
                return $"Error al enviar correo: {ex.Message}";
            }
        }

        private string? ResolverBrevoApiKey()
        {
            // En Render la variable se llama apiKeyBrevo.
            var key = configuration["Email:Brevo:ApiKey"]
                ?? configuration["apiKeyBrevo"]
                ?? configuration["BREVO_API_KEY"];
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        private string? ResolverFrom()
        {
            var from = configuration["Email:From"];
            if (string.IsNullOrWhiteSpace(from))
                from = configuration["EMAIL_FROM"];
            return string.IsNullOrWhiteSpace(from) ? null : from.Trim();
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
