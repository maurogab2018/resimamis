using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos
{
    public class MadreRepositorio
    {
        private readonly ApplicationDbContext db;
        public MadreRepositorio()
        {
            db = new ApplicationDbContext();
        }

        public List<MADRE> listarMadres()
        {
            return db.MADRE.Where(m=>m.Estado==true).ToList();
        }

        public bool registrarMadre(MADRE madre)
        {
            var existeMadre = db.MADRE.FirstOrDefault(m => m.Dni == madre.Dni);
            if (existeMadre != null)
                throw new ApplicationException("Madre existente con ese Dni");
            madre.Estado = true;
            db.MADRE.Add(madre);
            db.SaveChanges();
            return true;
        }

        public MADRE consultarMadre(int Dni)
        {
            var madre = db.MADRE.Include(m => m.Bebe).FirstOrDefault(m => m.IdMadre == Dni);
            if (madre == null)
                throw new ApplicationException("Madre no existente con ese Id");
            return madre;
        }
        public MADRE modificarMadre(MADRE madre, MADRE madreModificar)
        {
            var propiedades = typeof(MADRE).GetProperties();

            foreach (var prop in propiedades)
            {
                // Evitamos tocar la lista de bebés acá
                if (prop.Name == nameof(MADRE.Bebe))
                    continue;

                var nuevoValor = prop.GetValue(madre);
                var valorActual = prop.GetValue(madreModificar);

                if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                {
                    prop.SetValue(madreModificar, nuevoValor);
                }
            }

            // Procesamos los bebés
            if (madre.Bebe != null)
            {
                foreach (var bebeNuevo in madre.Bebe)
                {
                    var bebeExistente = madreModificar.Bebe.FirstOrDefault(b => b.ID == bebeNuevo.ID);

                    if (bebeExistente != null)
                    {
                        var propiedadesBebe = typeof(BEBE).GetProperties();

                        foreach (var prop in propiedadesBebe)
                        {
                            var nuevoValor = prop.GetValue(bebeNuevo);
                            var valorActual = prop.GetValue(bebeExistente);

                            if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                            {
                                prop.SetValue(bebeExistente, nuevoValor);
                            }
                        }
                    }
                    else
                    {
                        if (db.BEBE.FirstOrDefault(b=>b.Dni== bebeNuevo.Dni) == null )
                            madreModificar.Bebe.Add(bebeNuevo); // Nuevo bebé
                    }
                }
            }

            db.SaveChangesAsync();
            return madre;

        }


        public List<EstadisticaLocalidades> devolverEstadisticasLocalidades()
        {
            var madree = db.MADRE;
            var localidadd = db.LOCALIDAD;

            var resultado = localidadd
            .Join(madree, l => l.idLocalidad, m => m.Localidad, (l, m) => new { Localidad = l, Madre = m })
            .GroupBy(x => new { x.Localidad.nombre })
            .Select(g => new EstadisticaLocalidades()
            {
                NombreLocalidad = g.Key.nombre,
                CantidadMadres = g.Count()
            })
            .ToList();

            var a = 54;
            return resultado;
        }

        public List<EstadisticaEdadesMadres> devolverEstadisticasEdadesMadres()
        {
            List<MADRE> madres = listarMadres(); // Puedes reemplazar esto con la obtención real de datos desde tu base de datos.

            var resultado = madres
                .Where(m => m.FechaNacimiento != null)
                .GroupBy(m => (int)(DateTime.Now.Subtract(m.FechaNacimiento).Days / 365.25)) // Cálculo de la edad
                .Select(g => new EstadisticaEdadesMadres()
                {
                    Edad = g.Key,
                    CantidadMadres = g.Count()
                })
                .OrderBy(r => r.Edad)
                .ToList();
            return resultado;
        }

    }
}
