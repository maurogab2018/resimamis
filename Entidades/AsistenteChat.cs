namespace ResimamisBackend.Entidades;

public class AsistentePreguntaRequest
{
    public string? Pregunta { get; set; }
    public List<AsistenteMensaje>? Historial { get; set; }
}

public class AsistenteMensaje
{
    public string Rol { get; set; } = "";
    public string Contenido { get; set; } = "";
}

public class AsistentePreguntaRespuesta
{
    public string Respuesta { get; set; } = "";
    public List<string> HerramientasUsadas { get; set; } = new();
}

public class AsistenteEstadoRespuesta
{
    public bool Habilitado { get; set; }
    public string Proveedor { get; set; } = "OpenAI";
    public string Modelo { get; set; } = "";
    public string[] QuePuedeConsultar { get; set; } = [];
}
