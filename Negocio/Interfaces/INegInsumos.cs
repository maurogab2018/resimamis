using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegInsumos
{
    List<INSUMO> obtenerInsumos();
    INSUMO? obtenerInsumoPorId(int idInsumo);
    INSUMO registrarInsumo(INSUMO insumo);
    bool modificarInsumo(int idInsumo, INSUMO insumo);
    bool eliminarInsumo(int idInsumo);
    bool registrarMovimientoInsumos(MOVIMIENTOSTOCK movimiento);
    List<PROVEEDOR> obtenerProveedores();
    List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro);
    DetalleMovimiento obtenerMovimientoPorId(int idMovimiento);
    List<EstadisticaInsumo> obtenerEstadisticaInsumo();
}
