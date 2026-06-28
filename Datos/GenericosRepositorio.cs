using ResimamisBackend.Datos.Interfaces;

namespace ResimamisBackend.Datos
{
    public class GenericosRepositorio : IGenericosRepositorio
    {
        private readonly ApplicationDbContext db;

        public GenericosRepositorio(ApplicationDbContext db)
        {
            this.db = db;
        }

        public List<LOCALIDAD> obtenerLocalidades()
        {
            return db.LOCALIDAD.ToList();
        }

        public bool existeLocalidad(int id) => db.LOCALIDAD.Any(l => l.idLocalidad == id);
    }
}
