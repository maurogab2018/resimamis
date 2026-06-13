using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class VISITA
    {
        [Key]
        public int idVisita { get; set; }

        public int idBebe { get; set; }

        /// <summary>Nombre de quien visitó al bebé.</summary>
        public string nombreVisitante { get; set; } = string.Empty;

        /// <summary>Vínculo con el bebé (ej. Madre, Padre, Abuela).</summary>
        public string familiar { get; set; } = string.Empty;

        public DateTime fechaHoraVisita { get; set; }

        public string? observacion { get; set; }

        /// <summary>DNI u otro documento del visitante (opcional).</summary>
        public int? documentoVisitante { get; set; }

        /// <summary>Contacto del visitante (opcional).</summary>
        public long? telefonoVisitante { get; set; }

        /// <summary>Baja lógica: false = eliminada.</summary>
        public bool Activa { get; set; } = true;

        /// <summary>Momento en que se registró la visita en el sistema.</summary>
        public DateTime fechaRegistro { get; set; }

        [JsonIgnore]
        public virtual BEBE? Bebe { get; set; }
    }
}
