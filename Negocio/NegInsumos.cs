using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegInsumos : INegInsumos
    {
        private readonly IInsumoRepositorio insumoRepositorio;
        private readonly INegProveedores negProveedores;

        public NegInsumos(IInsumoRepositorio insumoRepositorio, INegProveedores negProveedores)
        {
            this.insumoRepositorio = insumoRepositorio;
            this.negProveedores = negProveedores;
        }

        private static void ValidarDatosInsumo(INSUMO insumo, IInsumoRepositorio repo)
        {
            if (insumo == null)
                throw new ApplicationException("Insumo inválido.");

            if (string.IsNullOrWhiteSpace(insumo.nombre))
                throw new ApplicationException("El nombre es obligatorio.");

            insumo.descripcion = string.IsNullOrWhiteSpace(insumo.descripcion)
                ? string.Empty
                : insumo.descripcion.Trim();

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

        public INSUMO registrarInsumo(INSUMO insumo)
        {
            ValidarDatosInsumo(insumo, insumoRepositorio);
            return insumoRepositorio.registrarInsumo(insumo);
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
            if (movimiento == null)
                throw new ApplicationException("Movimiento inválido.");
            movimiento.observacion = string.IsNullOrWhiteSpace(movimiento.observacion)
                ? string.Empty
                : movimiento.observacion.Trim();
            negProveedores.ValidarProveedorActivoParaMovimiento(movimiento.idProveedor);
            return insumoRepositorio.registrarMovimientoStock(movimiento);
        }

        public List<PROVEEDOR> obtenerProveedores()
        {
            return negProveedores.listarProveedoresActivos();
        }

        public List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro)
        {
            return insumoRepositorio.obtenerMovimientos(movimientoFiltro);
        }

        public DetalleMovimiento obtenerMovimientoPorId(int idMovimiento)
        {
            if (idMovimiento <= 0)
                throw new ApplicationException("Id de movimiento inválido.");
            return insumoRepositorio.obtenerMovimientoPorId(idMovimiento);
        }

        public List<EstadisticaInsumo> obtenerEstadisticaInsumo()
        {
            return insumoRepositorio.devolverEstadisticas();
        }
    }
}
