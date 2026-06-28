namespace ResimamisBackend.Entidades;

public class ResultadoAvisoStockMinimo
{
    public int cantidadInsumosBajoMinimo { get; set; }
    public List<InsumoBajoStockMinimo> insumos { get; set; } = new();
    public bool correoEnviado { get; set; }
    public List<string> destinatarios { get; set; } = new();
    public string? mensaje { get; set; }
}
