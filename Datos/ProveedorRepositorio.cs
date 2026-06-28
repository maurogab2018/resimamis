using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos
{
    public class ProveedorRepositorio : IProveedorRepositorio
    {
        private readonly ApplicationDbContext db;

        public ProveedorRepositorio(ApplicationDbContext db)
        {
            this.db = db;
        }

        public List<PROVEEDOR> listarProveedores()
        {
            return db.PROVEEDOR
                .AsNoTracking()
                .OrderBy(p => p.nombre)
                .ToList();
        }

        public List<PROVEEDOR> listarProveedoresActivos()
        {
            return db.PROVEEDOR
                .AsNoTracking()
                .Where(p => p.Activa)
                .OrderBy(p => p.nombre)
                .ToList();
        }

        public PROVEEDOR? obtenerPorId(int idProveedor)
        {
            return db.PROVEEDOR.AsNoTracking().FirstOrDefault(p => p.idProveedor == idProveedor);
        }

        public PROVEEDOR obtenerParaModificar(int idProveedor)
        {
            var proveedor = db.PROVEEDOR.FirstOrDefault(p => p.idProveedor == idProveedor);
            if (proveedor == null)
                throw new NotFoundException("Proveedor no encontrado con ese id.");
            return proveedor;
        }

        public bool registrarProveedor(PROVEEDOR proveedor)
        {
            db.PROVEEDOR.Add(proveedor);
            db.SaveChanges();
            return true;
        }

        public bool modificarProveedor(PROVEEDOR datos, PROVEEDOR existente)
        {
            existente.nombre = datos.nombre;
            existente.descripcion = datos.descripcion;
            existente.Activa = datos.Activa;
            db.SaveChanges();
            return true;
        }

        public bool eliminarProveedorLogico(int idProveedor)
        {
            var proveedor = obtenerParaModificar(idProveedor);
            if (!proveedor.Activa)
                return true;
            proveedor.Activa = false;
            db.SaveChanges();
            return true;
        }

        public bool existeOtroProveedorConNombre(string nombre, int? exceptIdProveedor = null)
        {
            var normalizado = nombre.Trim().ToLower();
            return db.PROVEEDOR.Any(p =>
                p.nombre.ToLower() == normalizado
                && (exceptIdProveedor == null || p.idProveedor != exceptIdProveedor));
        }

        public bool proveedorActivo(int idProveedor)
        {
            return db.PROVEEDOR.AsNoTracking()
                .Any(p => p.idProveedor == idProveedor && p.Activa);
        }
    }
}
