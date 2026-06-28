using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IInsumoRepositorio
{
    List<INSUMO> obtenerInsumos();
    INSUMO? obtenerInsumoPorIdSinTracking(int idInsumo);
    List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro);
    DetalleMovimiento obtenerMovimientoPorId(int idMovimiento);
    INSUMO consultarInsumo(int idInsumo);
    INSUMO obtenerInsumoParaModificar(int idInsumo);
    INSUMO registrarInsumo(INSUMO insumo);
    bool modificarInsumo(INSUMO parcial, INSUMO existente);
    bool eliminarInsumoLogico(int idInsumo);
    bool actualizarStock(INSUMO insumo, int cantidad);
    bool registrarMovimientoStock(MOVIMIENTOSTOCK movimiento);
    List<EstadisticaInsumo> devolverEstadisticas();
    List<INSUMO> obtenerInsumosBajoStockMinimo();
    bool IdEstadoEsDelAmbitoInsumos(int idEstado);
    bool IdEstadoEsEliminadoInsumos(int idEstado);
}
