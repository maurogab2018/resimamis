namespace ResimamisBackend.Datos
{
    public class GenericosRepositorio
    {
        private readonly ApplicationDbContext db;
        public GenericosRepositorio()
        {
            db = new ApplicationDbContext();
        }

        public List<LOCALIDAD> obtenerLocalidades()
        {
            return db.LOCALIDAD.ToList();
        }

        public bool existeLocalidad(int id) => db.LOCALIDAD.Any(l => l.idLocalidad == id);
    }
}
