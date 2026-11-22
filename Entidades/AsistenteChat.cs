namespace ResimamisBackend.Entidades;

public class AsistentePreguntaRequest
{
    public string? Pregunta { get; set; }
    public List<AsistenteMensaje>? Historial { get; set; }

    /// <summary>
    /// La acción que la coordinadora confirmó con el botón (la AccionPendiente de la respuesta anterior).
    /// El aviso push solo se manda con esto; generar asignaciones también acepta un "sí" escrito.
    /// </summary>
    public string? AccionConfirmada { get; set; }
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

    /// <summary>
    /// Acción que quedó esperando el OK de la coordinadora ("generar_asignaciones" o "avisar_push").
    /// El front muestra los botones Confirmar / Cancelar. Null si no hay nada pendiente.
    /// </summary>
    public string? AccionPendiente { get; set; }

    public int PreguntasHoy { get; set; }
    public int LimiteDiario { get; set; }

    /// <summary>Id del registro de esta pregunta, para calificarla (👍/👎). Null si no se pudo guardar.</summary>
    public int? IdRegistro { get; set; }
}

/// <summary>La usuaria ya hizo todas las preguntas del día (Asistente:MaxPreguntasDia).</summary>
public class LimiteAsistenteException : ApplicationException
{
    public int Limite { get; }

    public LimiteAsistenteException(int limite)
        : base($"Llegaste al límite de {limite} preguntas por hoy. Mañana podés seguir consultando.")
    {
        Limite = limite;
    }
}

public class AsistenteEstadoRespuesta
{
    public bool Habilitado { get; set; }

    /// <summary>OpenAI configurado. Si es false, el asistente igual responde con los datos del sistema.</summary>
    public bool ModeloDisponible { get; set; }
    public string Proveedor { get; set; } = "OpenAI";
    public string Modelo { get; set; } = "";
    public string[] QuePuedeConsultar { get; set; } = [];
    public int PreguntasHoy { get; set; }
    public int LimiteDiario { get; set; }
}

/// <summary>Calificación opcional de una respuesta: 1 = bien, -1 = mal, 0 = quitar el voto.</summary>
public class AsistenteCalificarRequest
{
    public int IdRegistro { get; set; }
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
}

public class AsistenteRegistroItem
{
    public int IdRegistro { get; set; }
    public DateTime Fecha { get; set; }
    public int Dni { get; set; }
    public string Pregunta { get; set; } = "";
    public string Respuesta { get; set; } = "";
    public string Herramientas { get; set; } = "";
    public string Resultado { get; set; } = "";
    public int DuracionMs { get; set; }
    public int Tokens { get; set; }
    public int? Calificacion { get; set; }
    public string? Comentario { get; set; }
}
