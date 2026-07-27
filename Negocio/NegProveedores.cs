using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegProveedores : INegProveedores
    {
        private readonly IProveedorRepositorio proveedorRepositorio;

        public NegProveedores(IProveedorRepositorio proveedorRepositorio)
        {
            this.proveedorRepositorio = proveedorRepositorio;
        }

        private static void ValidarProveedor(PROVEEDOR proveedor)
        {
            if (proveedor == null)
                throw new ApplicationException("Proveedor inválido.");
            if (string.IsNullOrWhiteSpace(proveedor.nombre))
                throw new ApplicationException("El nombre es obligatorio.");
            if (proveedor.nombre.Trim().Length > 200)
                throw new ApplicationException("El nombre no permite más de 200 caracteres.");
            if (!string.IsNullOrWhiteSpace(proveedor.descripcion) && proveedor.descripcion.Trim().Length > 500)
                throw new ApplicationException("La descripción no permite más de 500 caracteres.");
        }

        public List<PROVEEDOR> listarProveedores()
        {
            return proveedorRepositorio.listarProveedores();
        }

        public List<PROVEEDOR> listarProveedoresActivos()
        {
            return proveedorRepositorio.listarProveedoresActivos();
        }

        public PROVEEDOR consultarProveedor(int idProveedor)
        {
            var proveedor = proveedorRepositorio.obtenerPorId(idProveedor);
            if (proveedor == null)
                throw new NotFoundException("Proveedor inexistente con ese id.");
            return proveedor;
        }

        public bool registrarProveedor(PROVEEDOR proveedor)
        {
            ValidarProveedor(proveedor);
            proveedor.nombre = proveedor.nombre.Trim();
            proveedor.descripcion = string.IsNullOrWhiteSpace(proveedor.descripcion)
                ? string.Empty
                : proveedor.descripcion.Trim();
            proveedor.Activa = true;
            if (proveedorRepositorio.existeOtroProveedorConNombre(proveedor.nombre))
                throw new ConflictException("Ya existe un proveedor con ese nombre.");
            return proveedorRepositorio.registrarProveedor(proveedor);
        }

        public bool modificarProveedor(int idProveedor, PROVEEDOR proveedor)
        {
            ValidarProveedor(proveedor);
            proveedor.nombre = proveedor.nombre.Trim();
            proveedor.descripcion = string.IsNullOrWhiteSpace(proveedor.descripcion)
                ? string.Empty
                : proveedor.descripcion.Trim();
            var existente = proveedorRepositorio.obtenerParaModificar(idProveedor);
            if (proveedorRepositorio.existeOtroProveedorConNombre(proveedor.nombre, idProveedor))
                throw new ConflictException("Ya existe otro proveedor con ese nombre.");
            return proveedorRepositorio.modificarProveedor(proveedor, existente);
        }

        public bool eliminarProveedor(int idProveedor)
        {
            return proveedorRepositorio.eliminarProveedorLogico(idProveedor);
        }

        public void ValidarProveedorActivoParaMovimiento(int? idProveedor)
        {
            if (!idProveedor.HasValue)
                return;
            var proveedor = proveedorRepositorio.obtenerPorId(idProveedor.Value);
            if (proveedor == null)
                throw new NotFoundException("Proveedor no encontrado.");
            if (!proveedor.Activa)
                throw new ApplicationException("El proveedor no está activo.");
        }
    }
}
