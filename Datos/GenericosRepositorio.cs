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
    }
}
