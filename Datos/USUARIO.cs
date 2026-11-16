using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class USUARIO
    {
        [Key]
        public int IdUsuario { get; set; }
        public int Dni { get; set; }

        public string Contrasena { get; set; }

        public DateTime FechaCreacion { get; set; }

        public int IdVoluntaria { get; set; }

        /// <summary>Estado del usuario (tabla ESTADO, ámbito Usuarios).</summary>
        public int? idEstado { get; set; }

        [JsonIgnore]
        public virtual ESTADO? Estado { get; set; }

        /// <summary>
        /// Solo para el alta: rol que se le da a la voluntaria al crearle el usuario
        /// (2 = Voluntaria, 3 = Coordinadora). El rol se guarda en VOLUNTARIA.IdRol.
        /// </summary>
        [NotMapped]
        public int? IdRol { get; set; }
    }
}
