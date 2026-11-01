using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class MadreRepositorio : IMadreRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;

        public MadreRepositorio(ApplicationDbContext db, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
        }

        /// <summary>Madres activas y no marcadas como Eliminado (ámbito Madres).</summary>
        private IQueryable<MADRE> QueryMadresVisibles()
        {
            int? idEliminado = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Madres")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();

            if (idEliminado == null)
                return db.MADRE.Where(m => m.Estado);

            return db.MADRE.Where(m => m.Estado && m.IdEstado != idEliminado);
        }

        public List<MADRE> listarMadres()
        {
            return QueryMadresVisibles()
                .AsNoTracking()
                .Include(m => m.LocalidadDetalle)
                .Include(m => m.EstadoDetalle).ThenInclude(e => e!.ambito)
                .ToList();
        }

        public bool registrarMadre(MADRE madre)
        {
            var existeMadre = db.MADRE.FirstOrDefault(m => m.Dni == madre.Dni);
            if (existeMadre != null)
                throw new ApplicationException(
                    $"Madre existente con ese Dni (id {existeMadre.IdMadre}). Para otro bebé de la misma madre, registralo asociándolo a la madre existente.");
            madre.Estado = true;

            // Los bebés que vienen con el alta se insertan en el mismo SaveChanges, así que no
            // pasan por registrarBebe y hay que darles acá el estado inicial y el chequeo de DNI.
            if (madre.Bebe != null && madre.Bebe.Count > 0)
            {
                var idEstadoInicial = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Sin abrazar", "Bebes");
                foreach (var bebe in madre.Bebe)
                {
                    if (bebe.Dni.HasValue && db.BEBE.Any(b => b.Dni == bebe.Dni))
                        throw new ApplicationException($"Bebe existente con ese Dni ({bebe.Dni}).");
                    bebe.IdEstado = idEstadoInicial;
                }
            }

            db.MADRE.Add(madre);
            db.SaveChanges();
            return true;
        }

        public MADRE consultarMadre(int Dni)
        {
            var madre = db.MADRE
                .Include(m => m.Bebe).ThenInclude(b => b.Estado)
                .Include(m => m.Bebe).ThenInclude(b => b.Sala)
                .Include(m => m.EstadoDetalle)
                .Include(m => m.LocalidadDetalle)
                .FirstOrDefault(m => m.IdMadre == Dni);
            if (madre == null)
                throw new NotFoundException("Madre no encontrada con ese Id.");
            return madre;
        }

        /// <summary>Madre visible (no dada de baja) con ese DNI, con sus bebés; null si no hay.</summary>
        public MADRE? consultarMadrePorDni(int dni)
        {
            return QueryMadresVisibles()
                .AsNoTracking()
                .Include(m => m.Bebe!).ThenInclude(b => b.Estado)
                .Include(m => m.Bebe!).ThenInclude(b => b.Sala)
                .Include(m => m.EstadoDetalle)
                .Include(m => m.LocalidadDetalle)
                .FirstOrDefault(m => m.Dni == dni);
        }

        public bool estaDadaDeBaja(MADRE madre)
        {
            if (!madre.Estado)
                return true;
            var idEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Madres");
            return madre.IdEstado == idEliminado;
        }

        public MADRE modificarMadre(MADRE madre, MADRE madreModificar)
        {
            var propiedades = typeof(MADRE).GetProperties();

            foreach (var prop in propiedades)
            {
                if (prop.Name is nameof(MADRE.Bebe) or nameof(MADRE.EstadoDetalle) or nameof(MADRE.LocalidadDetalle))
                    continue;
                if (!prop.CanWrite)
                    continue;

                var nuevoValor = prop.GetValue(madre);
                if (ParchePropiedades.EsAusente(nuevoValor))
                    continue;

                var valorActual = prop.GetValue(madreModificar);
                if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                    prop.SetValue(madreModificar, nuevoValor);
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
                            if (!prop.CanWrite)
                                continue;
                            if (prop.Name is nameof(BEBE.Madre) or nameof(BEBE.Sala) or nameof(BEBE.Estado)
                                or nameof(BEBE.LocalidadDetalle) or nameof(BEBE.Asignaciones) or nameof(BEBE.Visitas)
                                or nameof(BEBE.NombreSala))
                                continue;

                            var nuevoValor = prop.GetValue(bebeNuevo);
                            if (ParchePropiedades.EsAusente(nuevoValor))
                                continue;

                            var valorActual = prop.GetValue(bebeExistente);
                            if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                                prop.SetValue(bebeExistente, nuevoValor);
                        }
                    }
                    else
                    {
                        // Nuevo bebé de una madre existente. Antes se descartaba en silencio si había
                        // otro bebé con el mismo Dni, y como la comparación también matcheaba null,
                        // cualquier hermano sin Dni se perdía. Además entraba sin estado inicial.
                        if (bebeNuevo.Dni > 0 && db.BEBE.Any(b => b.Dni == bebeNuevo.Dni))
                            throw new ApplicationException($"Bebe existente con ese Dni ({bebeNuevo.Dni}).");
                        bebeNuevo.IdEstado = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Sin abrazar", "Bebes");
                        madreModificar.Bebe.Add(bebeNuevo);
                    }
                }
            }

            db.SaveChanges();
            return madreModificar;

        }


        public List<EstadisticaLocalidades> devolverEstadisticasLocalidades()
        {
            var madree = QueryMadresVisibles();
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

        public bool eliminarMadreLogico(int idMadre)
        {
            var madre = db.MADRE.FirstOrDefault(m => m.IdMadre == idMadre);
            if (madre == null)
                throw new NotFoundException("Madre no encontrada con ese Id.");

            var idEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Madres");
            madre.IdEstado = idEliminado;
            madre.Estado = false;
            db.SaveChanges();
            return true;
        }

    }
}
