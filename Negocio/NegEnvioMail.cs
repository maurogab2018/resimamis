using System.Net;
using System.Net.Mail;

namespace ResimamisBackend.Negocio
{
    public class NegEnvioMail
    {
        public string sendMail(string to, string asunto, string body)
        {
            //var envioMail = new NegEnvioMail();
            //string body = @"<style>
            //            h1{color:dodgerblue;}
            //            h2{color:red;}
            //            </style>
            //            <h1>Este es el body del correo</h1></br>
            //            <h2>Este es el segundo párrafo</h2>";
            //envioMail.sendMail("chiarimassetti@gmail.com", "Este correo fue enviado via C-sharp prueba xd", body);
            string msge = "Error al enviar este correo. Por favor verifique los datos o intente más tarde.";
            string from = "maurogab.2023@hotmail.com";
            string displayName = "Resimamis";
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(from, displayName);
                mail.To.Add(to);

                mail.Subject = asunto;
                mail.Body = body;
                mail.IsBodyHtml = true;


                SmtpClient client = new SmtpClient("smtp.office365.com", 587); //Aquí debes sustituir tu servidor SMTP y el puerto
                client.Credentials = new NetworkCredential(from, "Belgrano1000");
                client.EnableSsl = true;//En caso de que tu servidor de correo no utilice cifrado SSL,poner en false

                client.Send(mail);
                msge = "¡Correo enviado exitosamente! Pronto te contactaremos.";
            }
            catch (Exception ex)
            {
                msge = ex.Message + ". Por favor verifica tu conexión a internet y que tus datos sean correctos e intenta nuevamente.";
            }

            return msge;
        }
    }
}
