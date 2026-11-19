namespace ResimamisBackend.Entidades
{
    /// <summary>Resultado del alta de usuario: si se creó y qué pasó con el mail de bienvenida.</summary>
    public class AltaUsuarioRespuesta
    {
        public bool Creado { get; set; }
        public bool MailEnviado { get; set; }
        /// <summary>Dirección a la que se mandó (o se intentó mandar) el mail.</summary>
        public string? MailDestino { get; set; }
        /// <summary>Por qué no salió el mail, en palabras para mostrar en pantalla.</summary>
        public string? MotivoMail { get; set; }
    }
}
