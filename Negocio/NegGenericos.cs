using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegGenericos : INegGenericos
    {
        private readonly IGenericosRepositorio genericosRepositorio;

        public NegGenericos(IGenericosRepositorio genericosRepositorio)
        {
            this.genericosRepositorio = genericosRepositorio;
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
