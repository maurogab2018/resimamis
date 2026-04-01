using ResimamisBackend.Datos;

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
    }
}
