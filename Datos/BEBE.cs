using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class BEBE
    {       
        [Key]
        public int ID { get; set; }
        public int? Dni { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        public string? Sexo { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        public string? LugarNacimiento { get; set; }

        public DateTime? FechaIngresoNEO { get; set; }

        public decimal? PesoNacimiento { get; set; }
        public decimal? PesoIngresoNEO { get; set; }
        public decimal? PesoDiaAbrazos { get; set; }
        public decimal? PesoAlta { get; set; }

        public string? DiagnosticoIngreso { get; set; }

        public string? DiagnosticoEgreso { get; set; }

        public int? IdSala { get; set; }
        
        public int? IdMadre { get; set; }

        public int? IdEstado { get; set; }

        [JsonIgnore]
        public virtual MADRE? Madre { get; set; }

        [JsonIgnore]
        public virtual SALA? Sala { get; set; }

        public virtual ESTADO? Estado { get; set; }

        [JsonIgnore]
        public List<ASIGNACION>? Asignaciones { get; set; }

        [NotMapped]
        public string? NombreSala
        {
            get { return Sala?.Nombre; }
        }

    }

}
