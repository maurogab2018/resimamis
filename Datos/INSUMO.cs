using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class INSUMO
    {
        [Key]
        public int idInsumo { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }

        public int stockMaximo { get; set; }
        public int stockMinimo { get; set; }

        public int stockActual { get; set; }

        /// <summary>Estado operativo del insumo (tabla ESTADO, ámbito Insumos). Baja lógica: Eliminado.</summary>
        public int? idEstado { get; set; }

        [JsonIgnore]
        public virtual ESTADO? Estado { get; set; }

        public virtual ICollection<DETALLEASIGNACION>? detalles { get; set; }
    }
}
