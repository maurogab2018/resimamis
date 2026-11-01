using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ResimamisBackend.Negocio
{
    public class NegRegistroActividad : INegRegistroActividad
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<NegRegistroActividad> logger;

        public NegRegistroActividad(
            ApplicationDbContext db,
            IEstadoRepositorio estadoRepositorio,
            IHttpContextAccessor httpContextAccessor,
            ILogger<NegRegistroActividad> logger)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }

        public void Registrar(string tipoMovimiento, string tipoRegistro, int idRegistro, string descripcion) =>
            RegistrarVarios(tipoMovimiento, tipoRegistro, new[] { (idRegistro, descripcion) });

        public void RegistrarVarios(string tipoMovimiento, string tipoRegistro, IEnumerable<(int IdRegistro, string Descripcion)> registros)
        {
            try
            {
                var (idResponsable, nombreResponsable) = ObtenerResponsable();
                var ahora = NegConversorFecha.ObtenerFechaArgentina();
                foreach (var (idRegistro, descripcion) in registros)
                {
                    db.REGISTROACTIVIDAD.Add(new REGISTROACTIVIDAD
                    {
                        fechaHora = ahora,
                        tipoMovimiento = tipoMovimiento,
                        tipoRegistro = tipoRegistro,
                        idRegistro = idRegistro,
                        descripcion = descripcion.Trim(),
                        idVoluntariaResponsable = idResponsable,
                        nombreResponsable = nombreResponsable
                    });
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // La operación ya quedó guardada; perder la línea de bitácora no debe convertirla en un 500.
                logger.LogError(ex, "No se pudo registrar la actividad {Movimiento} {Tipo}.", tipoMovimiento, tipoRegistro);
            }
        }

        public CierresAutomaticosRespuesta ListarCierresAutomaticos(DateTime fechaInicio, DateTime fechaFin)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var cierres = db.REGISTROACTIVIDAD.AsNoTracking()
                .Where(r => r.tipoMovimiento == TiposMovimientoActividad.CierreAutomatico
                            && r.fechaHora >= inicioUtc && r.fechaHora < finUtc)
                .OrderByDescending(r => r.fechaHora)
                .ToList()
                .Select(r => new CierreAutomaticoItem
                {
                    FechaHora = r.fechaHora,
                    IdAsignacion = r.idRegistro,
                    Descripcion = r.descripcion,
                    Origen = r.nombreResponsable == null
                        ? "Cierre diario automático"
                        : $"Botón de coordinación ({r.nombreResponsable})"
                })
                .ToList();

            return new CierresAutomaticosRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                Total = cierres.Count,
                Cierres = cierres
            };
        }

        /// <summary>
        /// Voluntaria del JWT. El claim es el DNI del USUARIO, que puede no coincidir con el de su
        /// voluntaria, así que se resuelve por USUARIO.IdVoluntaria. Null fuera de un request
        /// (p. ej. el cierre diario).
        /// </summary>
        private (int? Id, string? Nombre) ObtenerResponsable()
        {
            var user = httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(ClaimTypes.Name)?.Value;
            if (!int.TryParse(claim, out var dni) || dni <= 0)
                return (null, null);

            var idVoluntaria = db.USUARIO.AsNoTracking()
                .Where(u => u.Dni == dni)
                .Select(u => (int?)u.IdVoluntaria)
                .FirstOrDefault();
            var voluntaria = db.VOLUNTARIA.AsNoTracking()
                .Where(v => idVoluntaria != null ? v.IdVoluntaria == idVoluntaria : v.Dni == dni)
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido })
                .FirstOrDefault();
            return voluntaria == null
                ? (null, null)
                : (voluntaria.IdVoluntaria, $"{voluntaria.Nombre} {voluntaria.Apellido}".Trim());
        }

        public List<REGISTROACTIVIDAD> ListarPorRangoUtc(DateTime inicioUtc, DateTime finUtcExclusivo)
        {
            return db.REGISTROACTIVIDAD.AsNoTracking()
                .Where(r => r.fechaHora >= inicioUtc && r.fechaHora < finUtcExclusivo)
                .OrderBy(r => r.fechaHora)
                .ToList();
        }

        public HistorialBajasRespuesta ListarHistorialBajas(string? tipoRegistro)
        {
            var tipo = NormalizarTipoHistorial(tipoRegistro);
            var registros = new List<HistorialBajaItem>();

            // Última baja anotada por registro; una madre reactivada y vuelta a dar de baja muestra la última.
            var bajas = db.REGISTROACTIVIDAD.AsNoTracking()
                .Where(r => r.tipoMovimiento == TiposMovimientoActividad.Baja
                            && (tipo == null || r.tipoRegistro == tipo))
                .Select(r => new { r.tipoRegistro, r.idRegistro, r.fechaHora, r.nombreResponsable })
                .ToList()
                .GroupBy(r => (r.tipoRegistro, r.idRegistro))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.fechaHora).First());

            HistorialBajaItem ConBitacora(HistorialBajaItem item, DateTime? fechaRespaldo)
            {
                if (bajas.TryGetValue((item.TipoRegistro, item.IdRegistro), out var baja))
                {
                    item.FechaBaja = baja.fechaHora;
                    item.Responsable = baja.nombreResponsable;
                }
                else
                    item.FechaBaja = fechaRespaldo;
                return item;
            }

            if (tipo is null or TiposRegistroActividad.Madre)
            {
                var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Madres");
                registros.AddRange(db.MADRE.AsNoTracking()
                    .Where(m => !m.Estado || m.IdEstado == idElim)
                    .Select(m => new { m.IdMadre, m.Nombre, m.Apellido, m.Dni })
                    .ToList()
                    .Select(m => ConBitacora(new HistorialBajaItem
                    {
                        TipoRegistro = TiposRegistroActividad.Madre,
                        IdRegistro = m.IdMadre,
                        Nombre = m.Nombre,
                        Apellido = m.Apellido,
                        Dni = m.Dni
                    }, null)));
            }

            if (tipo is null or TiposRegistroActividad.Bebe)
            {
                var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Bebes");
                registros.AddRange(db.BEBE.AsNoTracking()
                    .Where(b => b.IdEstado == idElim || b.FechaSalida != null)
                    .Select(b => new { b.ID, b.nombre, b.apellido, b.Dni, b.FechaSalida })
                    .ToList()
                    .Select(b => ConBitacora(new HistorialBajaItem
                    {
                        TipoRegistro = TiposRegistroActividad.Bebe,
                        IdRegistro = b.ID,
                        Nombre = b.nombre ?? string.Empty,
                        Apellido = b.apellido ?? string.Empty,
                        Dni = b.Dni,
                        Motivo = b.FechaSalida.HasValue ? "Egreso del NEO" : null
                    }, b.FechaSalida)));
            }

            if (tipo is null or TiposRegistroActividad.Voluntaria)
            {
                var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Voluntarias");
                registros.AddRange(db.VOLUNTARIA.AsNoTracking()
                    .Where(v => v.IdEstado == idElim)
                    .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido, v.Dni, v.FechaFin })
                    .ToList()
                    .Select(v => ConBitacora(new HistorialBajaItem
                    {
                        TipoRegistro = TiposRegistroActividad.Voluntaria,
                        IdRegistro = v.IdVoluntaria,
                        Nombre = v.Nombre,
                        Apellido = v.Apellido,
                        Dni = v.Dni
                    }, v.FechaFin)));
            }

            var ordenados = registros
                .OrderByDescending(r => r.FechaBaja.HasValue)
                .ThenByDescending(r => r.FechaBaja)
                .ThenBy(r => r.Apellido)
                .ThenBy(r => r.Nombre)
                .ToList();

            return new HistorialBajasRespuesta
            {
                TipoRegistro = tipo,
                Total = ordenados.Count,
                Registros = ordenados
            };
        }

        /// <summary>Acepta "madre", "bebe", "bebé", "voluntaria" sin importar mayúsculas ni tildes.</summary>
        private static string? NormalizarTipoHistorial(string? tipoRegistro)
        {
            if (string.IsNullOrWhiteSpace(tipoRegistro))
                return null;

            var sinTildes = new string(tipoRegistro.Trim().ToLowerInvariant()
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            return sinTildes switch
            {
                "madre" or "madres" => TiposRegistroActividad.Madre,
                "bebe" or "bebes" => TiposRegistroActividad.Bebe,
                "voluntaria" or "voluntarias" => TiposRegistroActividad.Voluntaria,
                _ => throw new ApplicationException("Tipo de registro inválido. Use Madre, Bebé o Voluntaria.")
            };
        }
    }
}
