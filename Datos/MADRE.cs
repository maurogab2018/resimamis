using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class MADRE
    {
        [Key]
        public int IdMadre { get; set; }
        public string Nombre { get; set; }

        public DateTime FechaNacimiento { get; set; }

        public string Apellido { get; set; }

        public int Dni { get; set; }
        public int Localidad { get; set; }

        public int EstadoCivil { get; set; }

        public int CantidadHijos { get; set; }

        public bool Estado { get; set; }

        public string MotivoAbrazo { get; set; }

        public Int64 Celular { get; set; }


        public  List<BEBE>? Bebe {get; set;}

        //public int? IdBebe { get; set; }

    }
}
