using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos
{
    public class UsuarioRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly EstadoRepositorio estadoRepositorio;

        public UsuarioRepositorio()
        {
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
        }

        internal static bool EsUsuarioEliminado(USUARIO u) =>
            u.Estado != null
            && u.Estado.ambito != null
            && u.Estado.ambito.nombre == "Usuarios"
            && u.Estado.nombre == "Eliminado";

        internal static bool EsUsuarioOperativo(USUARIO u) => !EsUsuarioEliminado(u);

        private int IdEstadoEliminadoUsuarios() =>
            estadoRepositorio.ObtenerIdEstadoEliminado("Usuarios");

        public USUARIO? ObtenerPorId(int idUsuario, bool asNoTracking = false)
        {
            var q = db.USUARIO
                .Include(u => u.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(u => u.IdUsuario == idUsuario);
            return asNoTracking ? q.AsNoTracking().FirstOrDefault() : q.FirstOrDefault();
        }

        public USUARIO? ObtenerPorDni(int dni, bool asNoTracking = false)
        {
            var q = db.USUARIO
                .Include(u => u.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(u => u.Dni == dni);
            return asNoTracking ? q.AsNoTracking().FirstOrDefault() : q.FirstOrDefault();
        }

        public bool ExisteOtroUsuarioConDni(int dni, int exceptIdUsuario)
        {
            return db.USUARIO.Any(u => u.Dni == dni && u.IdUsuario != exceptIdUsuario);
        }

        public bool ExisteUsuarioOperativoConDni(int dni, int? exceptIdUsuario = null)
        {
            var idElim = IdEstadoEliminadoUsuarios();
            return db.USUARIO.Any(u =>
                u.Dni == dni
                && (exceptIdUsuario == null || u.IdUsuario != exceptIdUsuario)
                && (u.idEstado == null || u.idEstado != idElim));
        }

        public bool VoluntariaTieneUsuarioActivo(int idVoluntaria, int? exceptIdUsuario = null)
        {
            var idElim = IdEstadoEliminadoUsuarios();
            return db.USUARIO.Any(u =>
                u.IdVoluntaria == idVoluntaria
                && (exceptIdUsuario == null || u.IdUsuario != exceptIdUsuario)
                && (u.idEstado == null || u.idEstado != idElim));
        }

        public List<UsuarioListado> ListarUsuariosOperativos()
        {
            var idElim = IdEstadoEliminadoUsuarios();
            return db.USUARIO
                .AsNoTracking()
                .Include(u => u.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(u => u.idEstado == null || u.idEstado != idElim)
                .Join(
                    db.VOLUNTARIA.AsNoTracking(),
                    u => u.IdVoluntaria,
                    v => v.IdVoluntaria,
                    (u, v) => new UsuarioListado
                    {
                        IdUsuario = u.IdUsuario,
                        Dni = u.Dni,
                        IdVoluntaria = u.IdVoluntaria,
                        NombreVoluntaria = v.Nombre,
                        ApellidoVoluntaria = v.Apellido,
                        FechaCreacion = u.FechaCreacion,
                        idEstado = u.idEstado,
                        NombreEstado = u.Estado != null ? u.Estado.nombre : null
                    })
                .OrderBy(u => u.ApellidoVoluntaria)
                .ThenBy(u => u.NombreVoluntaria)
                .ToList();
        }

        public void GuardarCambios()
        {
            db.SaveChanges();
        }

        public bool EliminarLogico(int idUsuario)
        {
            var usuario = db.USUARIO
                .Include(u => u.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(u => u.IdUsuario == idUsuario);
            if (usuario == null)
                throw new ApplicationException("Usuario no existente con ese Id");
            if (EsUsuarioEliminado(usuario))
                return true;
            usuario.idEstado = estadoRepositorio.ObtenerIdEstadoEliminado("Usuarios");
            db.SaveChanges();
            return true;
        }
    }
}
