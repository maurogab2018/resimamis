namespace ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Datos;

public interface IProveedorRepositorio
{
    List<PROVEEDOR> listarProveedores();
    List<PROVEEDOR> listarProveedoresActivos();
    PROVEEDOR? obtenerPorId(int idProveedor);
    PROVEEDOR obtenerParaModificar(int idProveedor);
    bool registrarProveedor(PROVEEDOR proveedor);
    bool modificarProveedor(PROVEEDOR datos, PROVEEDOR existente);
    bool eliminarProveedorLogico(int idProveedor);
    bool existeOtroProveedorConNombre(string nombre, int? exceptIdProveedor = null);
    bool proveedorActivo(int idProveedor);
}
