using System.ComponentModel.DataAnnotations;

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

        //public int? IdRol { get; set; }
    }
}
