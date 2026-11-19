namespace ResimamisBackend.Entidades
{
    /// <summary>Lo que devuelve <c>PushSubscription.toJSON()</c> en el navegador.</summary>
    public class RequestSuscripcionPush
    {
        public string? endpoint { get; set; }
        public ClavesSuscripcionPush? keys { get; set; }
    }

    public class ClavesSuscripcionPush
    {
        public string? p256dh { get; set; }
        public string? auth { get; set; }
    }

    public class RequestMailVoluntaria
    {
        public string? asunto { get; set; }
        public string? mensaje { get; set; }
    }

    public class ResultadoPushVoluntaria
    {
        public int idVoluntaria { get; set; }
        public string nombre { get; set; } = "";
        public int dispositivos { get; set; }
        public int enviados { get; set; }
    }

    public class RequestDesuscripcionPush
    {
        public string? endpoint { get; set; }
    }
}
