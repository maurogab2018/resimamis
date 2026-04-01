using ResimamisBackend.Negocio.MaquinasEstado;

namespace ResimamisBackend.Entidades
{
    public class Bebe
    {
        public int iD { get; set; }
        public int? dni { get; set; }
        public string nombre { get; set; }
        public string sexo { get; set; }
        public DateTime fechaNacimiento { get; set; }
        public string lugarNacimiento { get; set; }
        public DateTime fechaIngresoNEO { get; set; }
        public decimal pesoNacimiento { get; set; }
        public decimal pesoIngresoNEO { get; set; }
        public decimal pesoDiaAbrazos { get; set; }
        public decimal pesoAlta { get; set; }
        public string diagnosticoIngreso { get; set; }
        public string diagnosticoEgreso { get; set; }
        public int salaInternacion { get; set; }
        public int? idMadre { get; set; }
        public EstadoBebe estado { get; set; }

    }
}
