using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Suscripción a notificaciones push de un navegador (Web Push). Una voluntaria puede tener
    /// varias: una por celular o computadora donde aceptó las notificaciones.
    /// </summary>
    public class PUSHSUSCRIPCION
    {
        [Key]
        public int idPushSuscripcion { get; set; }

        public int idVoluntaria { get; set; }

        /// <summary>URL del servicio de push del navegador. Identifica la suscripción.</summary>
        [MaxLength(1000)]
        public string endpoint { get; set; } = string.Empty;

        [MaxLength(200)]
        public string p256dh { get; set; } = string.Empty;

        [MaxLength(100)]
        public string auth { get; set; } = string.Empty;

        public DateTime fechaAlta { get; set; }
    }
}
