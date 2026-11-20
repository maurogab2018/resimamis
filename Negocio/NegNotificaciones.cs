using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using WebPush;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Avisos a las voluntarias: push del navegador (Web Push con claves VAPID, sin servicio externo)
    /// y mail por <see cref="INegEnvioMail"/>. Ninguno hace fallar la operación que lo dispara.
    /// Claves: VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY y opcional VAPID_SUBJECT (mailto:...).
    /// </summary>
    public class NegNotificaciones : INegNotificaciones
    {
        private readonly ApplicationDbContext db;
        private readonly IConfiguration configuration;
        private readonly IUsuarioRepositorio usuarioRepositorio;
        private readonly INegUsuarios negUsuarios;
        private readonly INegEnvioMail negEnvioMail;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly INegRegistroActividad registroActividad;

        public NegNotificaciones(
            ApplicationDbContext db,
            IConfiguration configuration,
            IUsuarioRepositorio usuarioRepositorio,
            INegUsuarios negUsuarios,
            INegEnvioMail negEnvioMail,
            IServiceScopeFactory scopeFactory,
            INegRegistroActividad registroActividad)
        {
            this.db = db;
            this.configuration = configuration;
            this.usuarioRepositorio = usuarioRepositorio;
            this.negUsuarios = negUsuarios;
            this.negEnvioMail = negEnvioMail;
            this.scopeFactory = scopeFactory;
            this.registroActividad = registroActividad;
        }

        private string UrlFront =>
            (configuration["FRONT_URL"] ?? "https://resimamis-tesis.vercel.app").TrimEnd('/');

        // ---------- Push: suscripciones ----------

        public string? ClavePublicaPush() => ResolverVapid()?.PublicKey;

        public void SuscribirPush(int dniSolicitante, RequestSuscripcionPush suscripcion)
        {
            var endpoint = suscripcion?.endpoint?.Trim();
            var p256dh = suscripcion?.keys?.p256dh?.Trim();
            var auth = suscripcion?.keys?.auth?.Trim();
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(p256dh) || string.IsNullOrEmpty(auth))
                throw new ApplicationException("Suscripción push incompleta.");
            if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || endpoint.Length > 1000)
                throw new ApplicationException("Suscripción push inválida.");

            var idVoluntaria = IdVoluntariaDe(dniSolicitante);
            // El mismo navegador puede pasar a otra voluntaria (celular compartido): se reasigna.
            var existente = db.PUSHSUSCRIPCION.FirstOrDefault(p => p.endpoint == endpoint);
            if (existente == null)
            {
                db.PUSHSUSCRIPCION.Add(new PUSHSUSCRIPCION
                {
                    idVoluntaria = idVoluntaria,
                    endpoint = endpoint,
                    p256dh = p256dh,
                    auth = auth,
                    fechaAlta = DateTime.UtcNow,
                });
            }
            else
            {
                existente.idVoluntaria = idVoluntaria;
                existente.p256dh = p256dh;
                existente.auth = auth;
                existente.fechaAlta = DateTime.UtcNow;
            }
            db.SaveChanges();
        }

        public void DesuscribirPush(int dniSolicitante, string endpoint)
        {
            var idVoluntaria = IdVoluntariaDe(dniSolicitante);
            var filas = db.PUSHSUSCRIPCION
                .Where(p => p.endpoint == endpoint && p.idVoluntaria == idVoluntaria)
                .ToList();
            if (filas.Count == 0)
                return;
            db.PUSHSUSCRIPCION.RemoveRange(filas);
            db.SaveChanges();
        }

        public int DispositivosConPush(int dniSolicitante)
        {
            var idVoluntaria = IdVoluntariaDe(dniSolicitante);
            return db.PUSHSUSCRIPCION.AsNoTracking().Count(p => p.idVoluntaria == idVoluntaria);
        }

        public int ProbarPush(int dniSolicitante)
        {
            var vapid = ResolverVapid() ?? throw new ApplicationException("Las notificaciones push no están configuradas.");
            var idVoluntaria = IdVoluntariaDe(dniSolicitante);
            var subs = db.PUSHSUSCRIPCION.AsNoTracking().Where(p => p.idVoluntaria == idVoluntaria).ToList();
            var aviso = new AvisoPush("Notificaciones activadas", "Así te vamos a avisar cuando te asignen un bebé.", $"{UrlFront}/tareas");
            var (enviados, vencidas) = EnviarPush(vapid, subs, aviso);
            BorrarSuscripciones(db, vencidas);
            return enviados;
        }

        // ---------- Aviso al asignar ----------

        public void NotificarAsignaciones(IEnumerable<RespuestaAsignaciones> asignaciones)
        {
            try
            {
                var lista = asignaciones?.Where(a => a != null && a.idBebe.HasValue).ToList() ?? new();
                if (lista.Count == 0)
                    return;

                // Solo push: sin claves VAPID o sin dispositivos suscriptos no se avisa (no hay mail de respaldo).
                var vapid = ResolverVapid();
                if (vapid == null)
                    return;

                var idsVol = lista.Select(a => a.idVoluntaria).Distinct().ToList();
                var subs = db.PUSHSUSCRIPCION.AsNoTracking()
                    .Where(p => idsVol.Contains(p.idVoluntaria))
                    .ToList()
                    .GroupBy(p => p.idVoluntaria)
                    .ToDictionary(g => g.Key, g => g.ToList());
                if (subs.Count == 0)
                    return;

                var idsBebe = lista.Select(a => a.idBebe!.Value).Distinct().ToList();
                var salas = db.BEBE.AsNoTracking()
                    .Where(b => idsBebe.Contains(b.ID))
                    .Select(b => new { b.ID, Sala = b.Sala != null ? b.Sala.Nombre : null })
                    .ToDictionary(b => b.ID, b => b.Sala);

                // Todo lo que se usa en segundo plano ya está en memoria: el DbContext de la request
                // no se toca después de responder.
                var trabajos = lista
                    .Where(a => subs.ContainsKey(a.idVoluntaria))
                    .Select(a =>
                    {
                        salas.TryGetValue(a.idBebe!.Value, out var sala);
                        sala ??= a.nombreSala;
                        return new
                        {
                            a.idVoluntaria,
                            subs = subs[a.idVoluntaria],
                            bebe = a.nombreBebe ?? "un bebé",
                            sala,
                        };
                    }).ToList();

                var urlTareas = $"{UrlFront}/tareas";
                _ = Task.Run(() =>
                {
                    var vencidas = new List<string>();
                    foreach (var t in trabajos)
                    {
                        try
                        {
                            var donde = string.IsNullOrWhiteSpace(t.sala) ? "" : $" en {t.sala}";
                            var aviso = new AvisoPush("Nueva asignación", $"Te asignaron a {t.bebe}{donde}.", urlTareas);
                            var (enviados, malas) = EnviarPush(vapid, t.subs, aviso);
                            vencidas.AddRange(malas);
                            Console.WriteLine($"[aviso asignación] voluntaria {t.idVoluntaria}: push a {enviados} dispositivo(s)");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[aviso asignación] voluntaria {t.idVoluntaria}: error {ex.Message}");
                        }
                    }

                    if (vencidas.Count > 0)
                    {
                        try
                        {
                            using var scope = scopeFactory.CreateScope();
                            BorrarSuscripciones(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), vencidas);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[aviso asignación] no se pudieron borrar suscripciones vencidas: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[aviso asignación] error preparando avisos: {ex.Message}");
            }
        }

        // ---------- Push con mensaje libre (asistente) ----------

        public List<ResultadoPushVoluntaria> DispositivosPorVoluntaria(IEnumerable<int> idsVoluntaria)
        {
            var ids = (idsVoluntaria ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
            var vols = db.VOLUNTARIA.AsNoTracking()
                .Where(v => ids.Contains(v.IdVoluntaria))
                .Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido })
                .ToList();
            var cuenta = db.PUSHSUSCRIPCION.AsNoTracking()
                .Where(p => ids.Contains(p.idVoluntaria))
                .GroupBy(p => p.idVoluntaria)
                .Select(g => new { g.Key, n = g.Count() })
                .ToDictionary(x => x.Key, x => x.n);
            return vols.Select(v => new ResultadoPushVoluntaria
            {
                idVoluntaria = v.IdVoluntaria,
                nombre = $"{v.Nombre} {v.Apellido}".Trim(),
                dispositivos = cuenta.GetValueOrDefault(v.IdVoluntaria),
            }).ToList();
        }

        public List<ResultadoPushVoluntaria> EnviarPushAVoluntarias(IEnumerable<int> idsVoluntaria, string titulo, string mensaje)
        {
            var vapid = ResolverVapid() ?? throw new ApplicationException("Las notificaciones push no están configuradas.");
            var resultado = DispositivosPorVoluntaria(idsVoluntaria);
            var ids = resultado.Select(r => r.idVoluntaria).ToList();
            var subs = db.PUSHSUSCRIPCION.AsNoTracking().Where(p => ids.Contains(p.idVoluntaria)).ToList();
            var aviso = new AvisoPush(titulo, mensaje, $"{UrlFront}/tareas");
            var vencidas = new List<string>();
            foreach (var r in resultado)
            {
                var (enviados, malas) = EnviarPush(vapid, subs.Where(s => s.idVoluntaria == r.idVoluntaria).ToList(), aviso);
                r.enviados = enviados;
                vencidas.AddRange(malas);
            }
            BorrarSuscripciones(db, vencidas);
            Console.WriteLine($"[push asistente] {resultado.Count} voluntaria(s), {resultado.Count(r => r.enviados > 0)} con aviso entregado");
            return resultado;
        }

        // ---------- Recordatorio de turno ----------

        /// <summary>Turnos ya recordados hoy ("fecha:idHorarioVoluntaria"), para no repetir el aviso.</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> TurnosRecordados = new();

        public int EnviarRecordatoriosTurno(TimeSpan anticipacion)
        {
            var vapid = ResolverVapid();
            if (vapid == null) return 0;

            var ahoraAr = NegConversorFecha.HoraArgentina(DateTime.UtcNow);
            var hoy = DateOnly.FromDateTime(ahoraAr);
            // DIA: 1 = Lunes … 7 = Domingo.
            var idDia = ((int)ahoraAr.DayOfWeek + 6) % 7 + 1;
            var horaActual = ahoraAr.TimeOfDay;
            var limite = horaActual + anticipacion;

            // Turnos de hoy que empiezan dentro de la ventana de anticipación.
            var turnos = (from vh in db.VOLUNTARIAHORARIO.AsNoTracking()
                          join h in db.HORARIO.AsNoTracking() on vh.IdHorario equals h.IdHorario
                          join v in db.VOLUNTARIA.AsNoTracking() on vh.IdVoluntaria equals v.IdVoluntaria
                          where vh.Activa && h.IdDia == idDia
                                && h.HoraIngreso > horaActual && h.HoraIngreso <= limite
                                && (v.Estado == null || v.Estado.nombre != "Eliminado")
                          select new { vh.IdHorarioVoluntaria, vh.IdVoluntaria, v.Nombre, h.Turno, h.HoraIngreso })
                .ToList()
                .Where(t => !TurnosRecordados.ContainsKey($"{hoy:yyyy-MM-dd}:{t.IdHorarioVoluntaria}"))
                .ToList();
            if (turnos.Count == 0) return 0;

            // A la que ya fichó la entrada no hace falta recordarle.
            var (inicioHoyUtc, finHoyUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var ids = turnos.Select(t => t.IdVoluntaria).Distinct().ToList();
            var yaVinieron = db.ASISTENCIA.AsNoTracking()
                .Where(a => a.IdVoluntaria != null && ids.Contains(a.IdVoluntaria.Value)
                            && a.FechaHoraIngreso >= inicioHoyUtc && a.FechaHoraIngreso < finHoyUtc)
                .Select(a => a.IdVoluntaria!.Value)
                .Distinct()
                .ToHashSet();
            var subs = db.PUSHSUSCRIPCION.AsNoTracking().Where(p => ids.Contains(p.idVoluntaria)).ToList();

            var enviadas = 0;
            var vencidas = new List<string>();
            foreach (var t in turnos)
            {
                TurnosRecordados.TryAdd($"{hoy:yyyy-MM-dd}:{t.IdHorarioVoluntaria}", 0);
                if (yaVinieron.Contains(t.IdVoluntaria)) continue;
                var susc = subs.Where(s => s.idVoluntaria == t.IdVoluntaria).ToList();
                if (susc.Count == 0) continue;
                var turno = string.IsNullOrWhiteSpace(t.Turno) ? "" : $" de la {t.Turno.Trim().ToLowerInvariant()}";
                var aviso = new AvisoPush(
                    "Recordatorio de turno",
                    $"Hola {t.Nombre}, tu turno{turno} empieza a las {t.HoraIngreso:hh\\:mm}. ¡Te esperamos!",
                    $"{UrlFront}/tareas");
                var (n, malas) = EnviarPush(vapid, susc, aviso);
                if (n > 0) enviadas++;
                vencidas.AddRange(malas);
            }
            BorrarSuscripciones(db, vencidas);

            // Se descartan las marcas de días anteriores.
            foreach (var k in TurnosRecordados.Keys.Where(k => !k.StartsWith($"{hoy:yyyy-MM-dd}:")))
                TurnosRecordados.TryRemove(k, out _);

            Console.WriteLine($"[push recordatorio] {turnos.Count} turno(s) por empezar, {enviadas} recordatorio(s) entregado(s)");
            return enviadas;
        }

        // ---------- Mail suelto a una voluntaria ----------

        public string EnviarMailAVoluntaria(int dniSolicitante, int idVoluntaria, string asunto, string mensaje)
        {
            negUsuarios.ValidarCoordinadora(dniSolicitante);

            asunto = (asunto ?? "").Trim();
            mensaje = (mensaje ?? "").Trim();
            if (asunto.Length == 0)
                throw new ApplicationException("Escribí un asunto.");
            if (mensaje.Length == 0)
                throw new ApplicationException("Escribí un mensaje.");
            if (asunto.Length > 150)
                throw new ApplicationException("El asunto no puede tener más de 150 caracteres.");
            if (mensaje.Length > 2000)
                throw new ApplicationException("El mensaje no puede tener más de 2000 caracteres.");

            var vol = db.VOLUNTARIA.AsNoTracking().FirstOrDefault(v => v.IdVoluntaria == idVoluntaria)
                ?? throw new NotFoundException("Voluntaria no encontrada.");
            if (string.IsNullOrWhiteSpace(vol.Mail))
                throw new ApplicationException("La voluntaria no tiene mail cargado.");
            if (!negEnvioMail.EstaConfigurado())
                throw new ApplicationException("El envío de mails no está configurado.");

            // El texto de la coordinadora va escapado; los saltos de línea se respetan.
            var cuerpoMensaje = H(mensaje).Replace("\r\n", "\n").Replace("\n", "<br/>");
            var resultado = negEnvioMail.EnviarMail(vol.Mail, asunto, PlantillaMail(vol.Nombre, cuerpoMensaje, null));
            var enviado = resultado.StartsWith("Correo enviado", StringComparison.OrdinalIgnoreCase);
            registroActividad.RegistrarMail(TiposRegistroActividad.Voluntaria, idVoluntaria, vol.Mail, asunto, enviado,
                enviado ? null : "El servidor de mails no aceptó el envío.");
            if (!enviado)
                throw new ApplicationException("No se pudo enviar el mail. Probá de nuevo en unos minutos.");
            Console.WriteLine($"[mail voluntaria] {idVoluntaria}: {resultado}");
            return resultado;
        }

        // ---------- Auxiliares ----------

        private sealed record AvisoPush(string title, string body, string url);

        private int IdVoluntariaDe(int dni)
        {
            var usuario = usuarioRepositorio.ObtenerPorDni(dni, asNoTracking: true)
                ?? throw new UnauthorizedException("Usuario no encontrado.");
            return usuario.IdVoluntaria;
        }

        private VapidDetails? ResolverVapid()
        {
            var publica = configuration["VAPID_PUBLIC_KEY"];
            var privada = configuration["VAPID_PRIVATE_KEY"];
            if (string.IsNullOrWhiteSpace(publica) || string.IsNullOrWhiteSpace(privada))
                return null;
            var subject = configuration["VAPID_SUBJECT"];
            if (string.IsNullOrWhiteSpace(subject))
            {
                var from = configuration["EMAIL_FROM"] ?? configuration["Email:From"];
                subject = string.IsNullOrWhiteSpace(from) ? UrlFront : $"mailto:{from.Trim()}";
            }
            return new VapidDetails(subject.Trim(), publica.Trim(), privada.Trim());
        }

        /// <summary>Devuelve cuántos llegaron y los endpoints que el navegador dio por vencidos.</summary>
        private static (int enviados, List<string> vencidas) EnviarPush(VapidDetails vapid, List<PUSHSUSCRIPCION> subs, AvisoPush aviso)
        {
            var client = new WebPushClient();
            var payload = JsonSerializer.Serialize(aviso);
            var enviados = 0;
            var vencidas = new List<string>();
            foreach (var s in subs)
            {
                try
                {
                    client.SendNotification(new PushSubscription(s.endpoint, s.p256dh, s.auth), payload, vapid);
                    enviados++;
                }
                catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
                {
                    vencidas.Add(s.endpoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[push] error enviando a voluntaria {s.idVoluntaria}: {ex.Message}");
                }
            }
            return (enviados, vencidas);
        }

        private static void BorrarSuscripciones(ApplicationDbContext contexto, List<string> endpoints)
        {
            if (endpoints.Count == 0)
                return;
            var filas = contexto.PUSHSUSCRIPCION.Where(p => endpoints.Contains(p.endpoint)).ToList();
            contexto.PUSHSUSCRIPCION.RemoveRange(filas);
            contexto.SaveChanges();
        }

        private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

        private string PlantillaMail(string? nombre, string cuerpoHtml, string? pie)
        {
            var saludo = string.IsNullOrWhiteSpace(nombre) ? "¡Hola!" : $"¡Hola {H(nombre)}!";
            var piePrrafo = string.IsNullOrWhiteSpace(pie) ? "" : $"<p style=\"font-size:13px\">{H(pie)}</p>";
            return $@"
<div style=""font-family:Arial,sans-serif;color:#152C70;max-width:480px"">
  <h2 style=""color:#7F00FF"">{saludo}</h2>
  <p>{cuerpoHtml}</p>
  {piePrrafo}
  <p><a href=""{H(UrlFront)}/login"" style=""color:#7F00FF"">Entrar a Voluntarias Abrazadoras</a></p>
</div>";
        }
    }
}
