using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    public class NegInsumos
    {
        private readonly InsumoRepositorio insumoRepositorio;
        public NegInsumos()
        {
            insumoRepositorio = new InsumoRepositorio();
        }


        public List<INSUMO> obtenerInsumos()
        {
            return insumoRepositorio.obtenerInsumos();
        }
        public bool registrarMovimientoInsumos(MOVIMIENTOSTOCK movimiento)
        {
            return insumoRepositorio.registrarMovimientoStock(movimiento);
        }
        public List<PROVEEDOR> obtenerProveedores()
        {
            return insumoRepositorio.obtenerProveedores();
        }
        public List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro)
        {
            return insumoRepositorio.obtenerMovimientos(movimientoFiltro);
        }

        public List<EstadisticaInsumo> obtenerEstadisticaInsumo()
        {
            return insumoRepositorio.devolverEstadisticas();
        }
    }
}
