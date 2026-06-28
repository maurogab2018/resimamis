using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos
{
    public class SalaRepositorio : ISalaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;

        public SalaRepositorio(ApplicationDbContext db, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
        }

        private int? IdEstadoEliminadoBebes()
        {
            try
            {
                return estadoRepositorio.ObtenerIdEstadoEliminado("Bebes");
            }
            catch
            {
                return null;
            }
        }

        public List<SALA> listarSalas()
        {
            return db.SALA
                .AsNoTracking()
                .OrderBy(s => s.Nombre)
                .ToList();
        }

        public List<SALA> listarSalasActivas()
        {
            return db.SALA
                .AsNoTracking()
                .Where(s => s.Activa)
                .OrderBy(s => s.Nombre)
                .ToList();
        }

        public SALA? obtenerPorId(int idSala)
        {
            return db.SALA.AsNoTracking().FirstOrDefault(s => s.IdSala == idSala);
        }

        public SALA obtenerParaModificar(int idSala)
        {
            var sala = db.SALA.FirstOrDefault(s => s.IdSala == idSala);
            if (sala == null)
                throw new NotFoundException("Sala no encontrada con ese id.");
            return sala;
        }

        public bool registrarSala(SALA sala)
        {
            db.SALA.Add(sala);
            db.SaveChanges();
            return true;
        }

        public bool modificarSala(SALA datos, SALA existente)
        {
            existente.Nombre = datos.Nombre;
            existente.Activa = datos.Activa;
            db.SaveChanges();
            return true;
        }

        public bool eliminarSalaLogica(int idSala)
        {
            var sala = obtenerParaModificar(idSala);
            if (!sala.Activa)
                return true;
            sala.Activa = false;
            db.SaveChanges();
            return true;
        }

        public bool existeOtraSalaConNombre(string nombre, int? exceptIdSala = null)
        {
            var normalizado = nombre.Trim().ToLower();
            return db.SALA.Any(s =>
                s.Nombre.ToLower() == normalizado
                && (exceptIdSala == null || s.IdSala != exceptIdSala));
        }

        public bool salaActiva(int idSala)
        {
            return db.SALA.AsNoTracking()
                .Any(s => s.IdSala == idSala && s.Activa);
        }

        public bool tieneBebesActivosAsignados(int idSala)
        {
            var idElim = IdEstadoEliminadoBebes();
            return db.BEBE.Any(b =>
                b.IdSala == idSala
                && (idElim == null || b.IdEstado == null || b.IdEstado != idElim));
        }
    }
}
