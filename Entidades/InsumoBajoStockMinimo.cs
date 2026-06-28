namespace ResimamisBackend.Entidades;

public class InsumoBajoStockMinimo
{
    public int idInsumo { get; set; }
    public string nombre { get; set; } = string.Empty;
    public int stockActual { get; set; }
    public int stockMinimo { get; set; }
    public int stockMaximo { get; set; }
}
