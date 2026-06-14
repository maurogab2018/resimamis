using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos
{
    public class VisitaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly BebeRepositorio bebeRepositorio;

        public VisitaRepositorio()
        {
            db = new ApplicationDbContext();
            bebeRepositorio = new BebeRepositorio();
        }

        private IQueryable<VISITA> QueryVisitasActivas() =>
            db.VISITA.AsNoTracking().Where(v => v.Activa);

        private static VisitaListado MapearListado(VISITA v, BEBE? bebe) => new()
        {
            idVisita = v.idVisita,
            idBebe = v.idBebe,
            nombreBebe = bebe?.nombre,
            apellidoBebe = bebe?.apellido,
            nombreVisitante = v.nombreVisitante,
            familiar = v.familiar,
            fechaHoraVisita = v.fechaHoraVisita,
            observacion = v.observacion,
            documentoVisitante = v.documentoVisitante,
            telefonoVisitante = v.telefonoVisitante,
            activa = v.Activa,
            fechaRegistro = v.fechaRegistro
        };

        public List<VisitaListado> listarVisitas()
        {
            return QueryVisitasActivas()
                .Join(
                    db.BEBE.AsNoTracking(),
                    v => v.idBebe,
                    b => b.ID,
                    (v, b) => MapearListado(v, b))
                .OrderByDescending(v => v.fechaHoraVisita)
                .ToList();
        }

        public List<VisitaListado> listarVisitasPorBebe(int idBebe)
        {
            bebeRepositorio.consultarBebe(idBebe);

            return QueryVisitasActivas()
                .Where(v => v.idBebe == idBebe)
                .Join(
                    db.BEBE.AsNoTracking(),
                    v => v.idBebe,
                    b => b.ID,
                    (v, b) => MapearListado(v, b))
                .OrderByDescending(v => v.fechaHoraVisita)
                .ToList();
        }

        public VISITA? obtenerPorId(int idVisita)
        {
            return db.VISITA
                .AsNoTracking()
                .Include(v => v.Bebe)
                .FirstOrDefault(v => v.idVisita == idVisita && v.Activa);
        }

        public VISITA obtenerParaModificar(int idVisita)
        {
            var visita = db.VISITA.FirstOrDefault(v => v.idVisita == idVisita && v.Activa);
            if (visita == null)
                throw new ApplicationException("Visita inexistente o dada de baja.");
            return visita;
        }

        public bool registrarVisita(VISITA visita)
        {
            db.VISITA.Add(visita);
            db.SaveChanges();
            return true;
        }

        public bool modificarVisita(VISITA datos, VISITA existente)
        {
            existente.idBebe = datos.idBebe;
            existente.nombreVisitante = datos.nombreVisitante;
            existente.familiar = datos.familiar;
            existente.fechaHoraVisita = datos.fechaHoraVisita;
            existente.observacion = datos.observacion;
            existente.documentoVisitante = datos.documentoVisitante;
            existente.telefonoVisitante = datos.telefonoVisitante;
            db.SaveChanges();
            return true;
        }

        public bool eliminarVisitaLogica(int idVisita)
        {
            var visita = db.VISITA.FirstOrDefault(v => v.idVisita == idVisita);
            if (visita == null)
                throw new ApplicationException("Visita inexistente con ese id.");
            if (!visita.Activa)
                return true;
            visita.Activa = false;
            db.SaveChanges();
            return true;
        }
    }
}
