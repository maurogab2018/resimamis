using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class ESTADO
    {
        [Key]

        public int idEstado { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        public int idAmbito { get; set; }
        public virtual AMBITO ambito { get; set; }

        [JsonIgnore]
        public ICollection<BEBE>? Bebes { get; set; }

        [JsonIgnore]
        public ICollection<VOLUNTARIA>? Voluntarias { get; set; }

        [JsonIgnore]
        public ICollection<MADRE>? Madres { get; set; }

        [JsonIgnore]
        public ICollection<ASIGNACION>? Asignaciones { get; set; }

        [JsonIgnore]
        public ICollection<INSUMO>? Insumos { get; set; }

        [JsonIgnore]
        public ICollection<ASISTENCIA>? Asistencias { get; set; }

        [JsonIgnore]
        public ICollection<USUARIO>? Usuarios { get; set; }


    }
}
