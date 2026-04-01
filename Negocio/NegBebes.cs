using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class NegBebes
    {
        private readonly BebeRepositorio repositorioBebe;
        public NegBebes()
        {
            repositorioBebe = new BebeRepositorio();
        }

        public List<BEBE> listarBebes()
        {
            return repositorioBebe.listarBebes();
        }

        public List<SALA> listarSalas()
        {
            return repositorioBebe.listarSalas();
        }

        public bool registrarBebe(BEBE bebe)
        {
            //aca van las validaciones
            bool registroBebe = repositorioBebe.registrarBebe(bebe);
            return registroBebe;
        }

        public BEBE consultarBebe(int id)
        {
            return repositorioBebe.consultarBebe(id);
        }

        public bool modificarBebe(BEBE bebe)
        {
            //validaciones
            var bebeModificar = repositorioBebe.consultarBebe(bebe.ID);
            if (bebe.nombre == null || bebe.nombre.Length > 50)
                throw new ApplicationException("Nombre inválido");
            if (bebe.nombre == null || bebe.apellido.Length > 50)
                throw new ApplicationException("Nombre inválido");
            if (bebe.Sexo == null)
                throw new ApplicationException("Debe ingresar sexo");
            
            return repositorioBebe.modificarBebe(bebe, bebeModificar);
        }
        public List<BEBE> listarBebesAbrazar()
        {
            return repositorioBebe.obtenerBebesAbrazar();
        }

    }
}
