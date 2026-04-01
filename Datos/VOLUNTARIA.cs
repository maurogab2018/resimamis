using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class VOLUNTARIA
    {
        [Key]
        public int IdVoluntaria { get; set; }
        public int Dni { get; set; }
        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public string Mail { get; set; }
        //public string? estadoVoluntaria { get; set; }

        public Int64 Celular { get; set; }
        public DateTime FechaInicio{ get; set; }
        public DateTime? FechaFin { get; set; }
        public int? IdEstado { get; set; }

        public int? IdRol { get; set; }
        //public string? rol { get; set; }

        [JsonIgnore]
        public virtual ESTADO? Estado { get; set; }

        [NotMapped]
        public string? rol
        {
            get { return RolInfo?.Nombre; }
        }

        [JsonIgnore]
        public virtual ROL? RolInfo { get; set; }
        [JsonIgnore]
        public List<ASISTENCIA>? Asistencias { get; set; }

        [JsonIgnore]
        public List<ASIGNACION>? Asignaciones { get; set; }

    }
}
