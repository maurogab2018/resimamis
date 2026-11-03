using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Bitácora de altas y bajas de registros (madres, bebés, voluntarias, insumos, proveedores,
    /// salas) y de los cierres automáticos de abrazos: cuándo pasó y quién lo hizo. Alimenta el
    /// historial de bajas, el reporte diario y la consulta de cierres automáticos.
    /// Sin responsable = lo hizo el sistema (cierre diario).
    /// El nombre del responsable se guarda como foto del momento para que el registro no cambie
    /// si después se edita o se da de baja a esa voluntaria.
    /// </summary>
    public class REGISTROACTIVIDAD
    {
        [Key]
        public int idRegistroActividad { get; set; }

        public DateTime fechaHora { get; set; }

        /// <summary><see cref="TiposMovimientoActividad"/>: Alta o Baja.</summary>
        public string tipoMovimiento { get; set; } = string.Empty;

        /// <summary><see cref="TiposRegistroActividad"/>: Madre, Bebé, Voluntaria, Insumo, Proveedor o Sala.</summary>
        public string tipoRegistro { get; set; } = string.Empty;

        /// <summary>Id del registro afectado en su propia tabla.</summary>
        public int idRegistro { get; set; }

        /// <summary>Identificación legible del registro (nombre, DNI) al momento del movimiento.</summary>
        public string descripcion { get; set; } = string.Empty;

        public int? idVoluntariaResponsable { get; set; }

        public string? nombreResponsable { get; set; }
    }

    public static class TiposMovimientoActividad
    {
        public const string Alta = "Alta";
        public const string Baja = "Baja";
        /// <summary>Abrazo cerrado por el sistema (cierre diario) o desde el botón de coordinación.</summary>
        public const string CierreAutomatico = "Cierre automático";
        /// <summary>Marca de que el cierre diario ya corrió ese día (idRegistro = cantidad cerrada).</summary>
        public const string EjecucionCierreDiario = "Ejecución cierre diario";
    }

    public static class TiposRegistroActividad
    {
        public const string Madre = "Madre";
        public const string Bebe = "Bebé";
        public const string Voluntaria = "Voluntaria";
        public const string Insumo = "Insumo";
        public const string Proveedor = "Proveedor";
        public const string Sala = "Sala";
        public const string Asignacion = "Asignación";
        public const string Sistema = "Sistema";
    }
}
