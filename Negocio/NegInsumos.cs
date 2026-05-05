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

        private static void ValidarDatosInsumo(INSUMO insumo, InsumoRepositorio repo)
        {
            if (insumo == null)
                throw new ApplicationException("Insumo inválido.");

            if (string.IsNullOrWhiteSpace(insumo.nombre))
                throw new ApplicationException("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(insumo.descripcion))
                throw new ApplicationException("La descripción es obligatoria.");

            if (insumo.stockMinimo < 0 || insumo.stockMaximo < 0 || insumo.stockActual < 0)
                throw new ApplicationException("Los stocks no pueden ser negativos.");
            if (insumo.stockMinimo > insumo.stockMaximo)
                throw new ApplicationException("El stock mínimo no puede superar el stock máximo.");
            if (insumo.stockActual > insumo.stockMaximo)
                throw new ApplicationException("El stock actual no puede superar el stock máximo.");

            if (insumo.idEstado.HasValue)
            {
                if (!repo.IdEstadoEsDelAmbitoInsumos(insumo.idEstado.Value))
                    throw new ApplicationException("El estado no es válido para el ámbito Insumos.");
                if (repo.IdEstadoEsEliminadoInsumos(insumo.idEstado.Value))
                    throw new ApplicationException("No se puede asignar el estado Eliminado desde la modificación; use el endpoint de baja lógica.");
            }
        }

        public List<INSUMO> obtenerInsumos()
        {
            return insumoRepositorio.obtenerInsumos();
        }

        public INSUMO? obtenerInsumoPorId(int idInsumo)
        {
            return insumoRepositorio.obtenerInsumoPorIdSinTracking(idInsumo);
        }

        public bool modificarInsumo(int idInsumo, INSUMO insumo)
        {
            ValidarDatosInsumo(insumo, insumoRepositorio);
            var existente = insumoRepositorio.obtenerInsumoParaModificar(idInsumo);
            if (insumo.idEstado == null)
                insumo.idEstado = existente.idEstado;
            return insumoRepositorio.modificarInsumo(insumo, existente);
        }

        public bool eliminarInsumo(int idInsumo)
        {
            return insumoRepositorio.eliminarInsumoLogico(idInsumo);
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
