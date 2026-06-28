using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegProveedores
{
    List<PROVEEDOR> listarProveedores();
    List<PROVEEDOR> listarProveedoresActivos();
    PROVEEDOR consultarProveedor(int idProveedor);
    bool registrarProveedor(PROVEEDOR proveedor);
    bool modificarProveedor(int idProveedor, PROVEEDOR proveedor);
    bool eliminarProveedor(int idProveedor);
    void ValidarProveedorActivoParaMovimiento(int? idProveedor);
}
