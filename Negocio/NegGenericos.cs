using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    public class NegGenericos
    {
        private readonly GenericosRepositorio genericosRepositorio;

        public NegGenericos()
        {
            genericosRepositorio= new GenericosRepositorio();
        }

        public List<LOCALIDAD> obtenerLocalidades()
        {
            return genericosRepositorio.obtenerLocalidades();
        }

        public List<EstadoCivilItem> obtenerEstadosCiviles()
        {
            return new List<EstadoCivilItem>
            {
                new EstadoCivilItem { id = 1, nombre = "Soltera/o" },
                new EstadoCivilItem { id = 2, nombre = "Casada/o" },
                new EstadoCivilItem { id = 3, nombre = "Unión convivencial" },
                new EstadoCivilItem { id = 4, nombre = "Divorciada/o" },
                new EstadoCivilItem { id = 5, nombre = "Viuda/o" },
                new EstadoCivilItem { id = 6, nombre = "Separada/o" },
            };
        }
    }
}
