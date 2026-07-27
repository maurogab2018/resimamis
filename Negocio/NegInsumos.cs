using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ResimamisBackend.Negocio
{
    public class NegInsumos : INegInsumos
    {
        private readonly IInsumoRepositorio insumoRepositorio;
        private readonly INegProveedores negProveedores;
        private readonly INegEnvioMail negEnvioMail;
        private readonly IVoluntariaRepositorio voluntariaRepositorio;
        private readonly IConfiguration configuration;

        public NegInsumos(
            IInsumoRepositorio insumoRepositorio,
            INegProveedores negProveedores,
            INegEnvioMail negEnvioMail,
            IVoluntariaRepositorio voluntariaRepositorio,
            IConfiguration configuration)
        {
            this.insumoRepositorio = insumoRepositorio;
            this.negProveedores = negProveedores;
            this.negEnvioMail = negEnvioMail;
            this.voluntariaRepositorio = voluntariaRepositorio;
            this.configuration = configuration;
        }

        private static void ValidarDatosInsumo(INSUMO insumo, IInsumoRepositorio repo)
        {
            if (insumo == null)
                throw new ApplicationException("Insumo inválido.");

            if (string.IsNullOrWhiteSpace(insumo.nombre))
                throw new ApplicationException("El nombre es obligatorio.");
            insumo.nombre = insumo.nombre.Trim();
            if (insumo.nombre.Length > 100)
                throw new ApplicationException("El nombre no permite más de 100 caracteres.");

            insumo.descripcion = string.IsNullOrWhiteSpace(insumo.descripcion)
                ? string.Empty
                : insumo.descripcion.Trim();
            if (insumo.descripcion.Length > 500)
                throw new ApplicationException("La descripción no permite más de 500 caracteres.");

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
            if (movimiento.idInsumo <= 0)
                throw new ApplicationException("Debe indicar el insumo.");
            if (!movimiento.cantidad.HasValue || movimiento.cantidad.Value <= 0)
                throw new ApplicationException("La cantidad debe ser mayor a 0.");
            var esEntrada = (movimiento.esEntrada ?? "").Trim().ToUpperInvariant();
            if (esEntrada is not ("S" or "N"))
                throw new ApplicationException("esEntrada debe ser S (entrada) o N (salida).");
            movimiento.esEntrada = esEntrada;
            movimiento.observacion = string.IsNullOrWhiteSpace(movimiento.observacion)
                ? string.Empty
                : movimiento.observacion.Trim();
            if (movimiento.observacion.Length > 500)
                throw new ApplicationException("La observación no permite más de 500 caracteres.");
            negProveedores.ValidarProveedorActivoParaMovimiento(movimiento.idProveedor);
            var ok = insumoRepositorio.registrarMovimientoStock(movimiento);
            if (ok)
            {
                try
                {
                    enviarAvisoStockMinimo();
                }
                catch
                {
                    // El movimiento no debe fallar si el mail falla.
                }
            }
            return ok;
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

        public List<InsumoBajoStockMinimo> obtenerInsumosBajoStockMinimo()
        {
            return insumoRepositorio.obtenerInsumosBajoStockMinimo()
                .Select(MapInsumoBajoStock)
                .ToList();
        }

        public ResultadoAvisoStockMinimo enviarAvisoStockMinimo()
        {
            var insumos = obtenerInsumosBajoStockMinimo();
            var resultado = new ResultadoAvisoStockMinimo
            {
                cantidadInsumosBajoMinimo = insumos.Count,
                insumos = insumos
            };

            if (insumos.Count == 0)
            {
                resultado.mensaje = "No hay insumos bajo stock mínimo.";
                return resultado;
            }

            var destinatarios = ResolverDestinatariosAvisoStock();
            resultado.destinatarios = destinatarios;

            if (destinatarios.Count == 0)
            {
                resultado.mensaje = "Hay insumos bajo mínimo pero no hay destinatarios configurados (Email:AvisoStockMinimo:Destinatarios o mails de coordinadoras).";
                return resultado;
            }

            if (!negEnvioMail.EstaConfigurado())
            {
                resultado.mensaje = "Correo no configurado (Email:Enabled y Smtp). Revise appsettings o variables de entorno.";
                return resultado;
            }

            var asunto = configuration["Email:AvisoStockMinimo:Asunto"]
                ?? "Resimamis — insumos bajo stock mínimo";
            var cuerpo = ArmarCuerpoAvisoStock(insumos);
            var respuestaEnvio = negEnvioMail.EnviarMail(destinatarios, asunto, cuerpo);
            resultado.correoEnviado = respuestaEnvio.StartsWith("Correo enviado", StringComparison.OrdinalIgnoreCase);
            resultado.mensaje = respuestaEnvio;
            return resultado;
        }

        private static InsumoBajoStockMinimo MapInsumoBajoStock(INSUMO i) => new()
        {
            idInsumo = i.idInsumo,
            nombre = i.nombre,
            stockActual = i.stockActual,
            stockMinimo = i.stockMinimo,
            stockMaximo = i.stockMaximo
        };

        private List<string> ResolverDestinatariosAvisoStock()
        {
            var desdeConfig = configuration.GetSection("Email:AvisoStockMinimo:Destinatarios").Get<string[]>() ?? Array.Empty<string>();
            var lista = desdeConfig
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .ToList();

            if (lista.Count > 0)
                return lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return voluntariaRepositorio.listarVoluntarias()
                .Where(v => RolesVoluntaria.EsCoordinadora(v.IdRol, v.RolInfo?.Nombre))
                .Select(v => v.Mail)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ArmarCuerpoAvisoStock(IReadOnlyList<InsumoBajoStockMinimo> insumos)
        {
            var sb = new StringBuilder();
            sb.Append("<h2>Resimamis — aviso de stock mínimo</h2>");
            sb.Append("<p>Los siguientes insumos están en o por debajo del stock mínimo configurado:</p>");
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
            sb.Append("<tr><th>Insumo</th><th>Stock actual</th><th>Stock mínimo</th><th>Stock máximo</th></tr>");
            foreach (var i in insumos)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(i.nombre)}</td>");
                sb.Append($"<td>{i.stockActual}</td>");
                sb.Append($"<td>{i.stockMinimo}</td>");
                sb.Append($"<td>{i.stockMaximo}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append($"<p><small>Generado: {NegConversorFecha.ObtenerFechaArgentina():yyyy-MM-dd HH:mm} (AR)</small></p>");
            return sb.ToString();
        }
    }
}
