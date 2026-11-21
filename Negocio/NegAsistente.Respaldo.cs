using System.Text;
using System.Text.Json;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Plan B del asistente: si OpenAI no está configurado, se cae, tarda o devuelve vacío, la
    /// coordinadora igual recibe una respuesta con datos reales de Resimamis armada desde la base.
    /// Detecta el tema por palabras clave (insumos, bebés, voluntarias, visitas, abrazos, mamás).
    /// Cada bloque está aislado: si una consulta falla, se sigue con las otras.
    /// </summary>
    public partial class NegAsistente
    {
        private static readonly string[] PalabrasVacias =
        [
            "cuanto", "cuantos", "cuanta", "cuantas", "hay", "quedan", "queda", "tenemos", "tengo", "stock",
            "insumo", "insumos", "deposito", "unidades", "cantidad", "del", "las", "los", "que", "para",
            "con", "por", "una", "uno", "unos", "unas", "decime", "dame", "mostrame", "sabes", "saber",
            "todavia", "ahora", "hoy", "esta", "este", "estan", "tiene", "tienen", "algun", "alguna",
            "mas", "menos", "como", "donde", "cual", "cuales", "quiero", "necesito", "podes", "puedo",
            "bebe", "bebes", "beba", "voluntaria", "voluntarias", "mama", "mamas", "madre", "madres",
            "abrazo", "abrazos", "visita", "visitas", "sala", "salas", "dia", "semana", "mes"
        ];

        private AsistentePreguntaRespuesta RespuestaDeRespaldo(
            string pregunta,
            int preguntasHoy,
            List<string>? usadas = null)
        {
            string texto;
            try
            {
                texto = ArmarTextoRespaldo(pregunta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[asistente] el respaldo también falló: {ex.Message}");
                texto = "Soy el asistente de Resimamis. En este momento no pude consultar los datos del sistema. " +
                        "Probá de nuevo en un minuto; mientras tanto podés ver el stock en Insumos, los bebés en Bebés " +
                        "y la operación del día en el panel de Coordinación.";
            }

            return new AsistentePreguntaRespuesta
            {
                Respuesta = LimpiarFormato(texto),
                HerramientasUsadas = (usadas ?? new List<string>()).Append("respaldo_datos_sistema")
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                AccionPendiente = null,
                PreguntasHoy = preguntasHoy,
                LimiteDiario = LimiteDiario(),
            };
        }

        /// <summary>Abreviaturas y errores comunes al escribir rápido → palabra que entiende el detector de temas.</summary>
        private static readonly Dictionary<string, string> Abreviaturas = new()
        {
            ["vol"] = "voluntarias", ["volu"] = "voluntarias", ["volun"] = "voluntarias", ["volunt"] = "voluntarias",
            ["voluns"] = "voluntarias", ["volus"] = "voluntarias", ["vols"] = "voluntarias", ["voluntas"] = "voluntarias",
            ["lib"] = "libres", ["libs"] = "libres", ["lbs"] = "libres", ["libr"] = "libres",
            ["bb"] = "bebes", ["bbs"] = "bebes", ["bbes"] = "bebes", ["bbe"] = "bebes", ["beb"] = "bebes", ["bebs"] = "bebes",
            ["asist"] = "asistencia", ["asis"] = "asistencia", ["fichj"] = "fichajes",
            ["abz"] = "abrazos", ["abr"] = "abrazos", ["abra"] = "abrazos", ["abrz"] = "abrazos", ["abrzs"] = "abrazos",
            ["vis"] = "visitas", ["visi"] = "visitas", ["vists"] = "visitas",
            ["coord"] = "coordinadoras", ["coordi"] = "coordinadoras", ["coords"] = "coordinadoras",
            ["stk"] = "stock", ["insum"] = "insumos", ["ins"] = "insumos",
            ["mam"] = "mamas", ["mams"] = "mamas", ["mami"] = "mamas", ["mamis"] = "mamas",
        };

        private static string ExpandirAbreviaturas(string normalizado) =>
            string.Join(' ', normalizado
                .Split(new[] { ' ', ',', '.', '?', '!', '¿', '¡', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => Abreviaturas.TryGetValue(p, out var largo) ? largo : p));

        private string ArmarTextoRespaldo(string pregunta)
        {
            var t = " " + ExpandirAbreviaturas(SinAcentos(pregunta)) + " ";
            bool Tiene(params string[] claves) => claves.Any(c => t.Contains(c));

            var bloques = new List<string>();
            void Sumar(Func<string?> bloque)
            {
                try
                {
                    var b = bloque();
                    if (!string.IsNullOrWhiteSpace(b)) bloques.Add(b.Trim());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[asistente] respaldo: bloque falló: {ex.Message}");
                }
            }

            // Insumos: por palabra clave o porque la pregunta nombra un insumo que existe ("cuántos chupetes hay").
            List<INSUMO>? insumosNombrados = null;
            try
            {
                insumosNombrados = InsumosQueCoinciden(pregunta, soloNombres: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[asistente] respaldo: no se pudo leer insumos: {ex.Message}");
            }

            var temaInsumos = insumosNombrados is { Count: > 0 }
                || Tiene("insumo", "stock", "deposito", "reponer", "bajo el minimo", "bajo minimo", "faltan", "falta ");
            var temaBebes = Tiene("bebe", "beba", " sala", "internad", " neo", "sin abrazo", "no recibi");
            var temaVoluntarias = Tiene("voluntari", "fich", "asistencia", " vino", "vinieron", "presente", "libres");
            var temaVisitas = Tiene("visita");
            var temaAbrazos = Tiene("abrazo", "asignac");
            var temaMamas = Tiene("mama", "madre");

            if (temaInsumos) Sumar(() => BloqueInsumos(t, insumosNombrados));
            if (temaBebes) Sumar(() => BloqueBebes(t));
            if (temaVoluntarias && t.Contains("libre")) Sumar(BloqueVoluntariasLibres);
            if (temaVoluntarias) Sumar(BloqueVoluntarias);
            if (temaVisitas) Sumar(BloqueVisitas);
            if (temaMamas) Sumar(BloqueMamas);
            if (temaAbrazos || bloques.Count == 0) Sumar(BloqueDiaDeHoy);

            var sb = new StringBuilder();
            var reconocido = temaInsumos || temaBebes || temaVoluntarias || temaVisitas || temaAbrazos || temaMamas;
            if (!reconocido)
                sb.Append("Soy el asistente de Resimamis y respondo sobre el programa de abrazos: bebés, voluntarias, " +
                          "abrazos, asistencias, visitas, mamás e insumos. Te dejo cómo viene el día:\n\n");

            if (bloques.Count == 0)
                throw new ApplicationException("Ningún bloque de datos respondió.");

            sb.Append(string.Join("\n\n", bloques));
            sb.Append("\n\nPodés preguntarme, por ejemplo: **¿cuántos chupetes hay?**, **¿qué bebés no recibieron abrazo hoy?** o **¿quién fichó hoy?**");
            return sb.ToString();
        }

        private string? BloqueInsumos(string t, List<INSUMO>? nombrados)
        {
            if (nombrados is { Count: > 0 })
            {
                var lineas = nombrados.Take(10).Select(i =>
                    $"- **{i.nombre}**: {i.stockActual} unidades (mínimo {i.stockMinimo})" +
                    (i.stockActual <= i.stockMinimo ? " — **bajo el mínimo, conviene reponer**" : ""));
                var encabezado = nombrados.Count == 1
                    ? $"Hay **{nombrados[0].stockActual} unidades** de **{nombrados[0].nombre}** en el depósito."
                    : "Stock de los insumos que coinciden:";
                return encabezado + "\n" + string.Join("\n", lineas);
            }

            var bajos = negInsumos.obtenerInsumosBajoStockMinimo() ?? new List<InsumoBajoStockMinimo>();
            var todos = negInsumos.obtenerInsumos() ?? new List<INSUMO>();
            var sb = new StringBuilder();
            sb.Append($"Hay **{todos.Count} insumos** cargados");
            sb.Append(bajos.Count == 0
                ? " y **ninguno está bajo el mínimo**."
                : $" y **{bajos.Count} están bajo el mínimo**:\n" +
                  string.Join("\n", bajos.Take(15).Select(b => $"- **{b.nombre}**: {b.stockActual} (mínimo {b.stockMinimo})")));
            if (!t.Contains("bajo") && !t.Contains("falta") && !t.Contains("reponer") && todos.Count > 0)
            {
                sb.Append("\n\nStock actual:\n");
                sb.Append(string.Join("\n", todos.OrderBy(i => i.nombre).Take(15)
                    .Select(i => $"- {i.nombre}: {i.stockActual}")));
                if (todos.Count > 15) sb.Append($"\n- … y {todos.Count - 15} más.");
            }
            return sb.ToString();
        }

        private string? BloqueBebes(string t)
        {
            if (t.Contains("sin abrazo") || t.Contains("no recibi"))
            {
                var c = negDashboard.ObtenerCoberturaHoy();
                var sb = new StringBuilder(
                    $"Hoy ({c.Fecha:dd/MM}) recibieron abrazo **{c.BebesConAbrazoFinalizadoHoy} de {c.TotalBebesActivos} bebés** " +
                    $"(**{c.PorcentajeCobertura:0.#}%** de cobertura).");
                if (c.BebesSinAbrazoHoy.Count > 0)
                {
                    sb.Append("\nTodavía sin abrazo:\n");
                    sb.Append(string.Join("\n", c.BebesSinAbrazoHoy.Take(15).Select(b =>
                        $"- [{($"{b.Nombre} {b.Apellido}").Trim()}](bebe:{b.IdBebe})" +
                        (string.IsNullOrWhiteSpace(b.NombreSala) ? "" : $" — {b.NombreSala}"))));
                }
                return sb.ToString();
            }

            var bebes = negBebes.listarBebes() ?? new List<BEBE>();
            var porSala = bebes.GroupBy(b => b.Sala?.Nombre ?? "Sin sala")
                .OrderByDescending(g => g.Count())
                .Select(g => $"- {g.Key}: {g.Count()}");
            return $"Hay **{bebes.Count} bebés activos** en el programa.\n" + string.Join("\n", porSala);
        }

        private string? BloqueVoluntarias()
        {
            var asistencias = negAsistencia.consultarAsistenciasFechahoy() ?? new List<ASISTENCIA>();
            var total = (negVoluntaria.listarVoluntarias() ?? new List<VOLUNTARIA>()).Count;
            var sb = new StringBuilder($"Hay **{total} voluntarias** cargadas. Hoy ficharon asistencia **{asistencias.Count}**");
            if (asistencias.Count > 0)
            {
                sb.Append(":\n");
                sb.Append(string.Join("\n", asistencias.Take(15).Select(a =>
                {
                    var nombre = $"{a.Voluntaria?.Nombre} {a.Voluntaria?.Apellido}".Trim();
                    var ingreso = a.FechaHoraIngreso is { } f ? NegConversorFecha.HoraArgentina(f).ToString("HH:mm") : "-";
                    return $"- [{nombre}](voluntaria:{a.IdVoluntaria}) — ingresó {ingreso}" +
                           (a.FechaHoraSalida.HasValue ? "" : " (sigue en el hospital)");
                })));
            }
            else
            {
                sb.Append('.');
            }
            return sb.ToString();
        }

        private string? BloqueVoluntariasLibres()
        {
            var libres = negVoluntaria.listarVoluntariasLibres1() ?? new List<VOLUNTARIA>();
            if (libres.Count == 0)
                return "Ahora **no hay voluntarias libres** para asignar abrazo (ninguna presente sin asignación).";
            return $"Hay **{libres.Count} voluntarias libres** para asignar abrazo:\n" + string.Join("\n", libres.Take(15).Select(v =>
                $"- [{($"{v.Nombre} {v.Apellido}").Trim()}](voluntaria:{v.IdVoluntaria})"));
        }

        private string? BloqueVisitas()
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var hoy = (negVisitas.listarVisitas() ?? new List<VisitaListado>())
                .Where(v => v.activa && v.fechaHoraVisita >= inicioUtc && v.fechaHoraVisita < finUtc)
                .OrderByDescending(v => v.fechaHoraVisita)
                .ToList();
            if (hoy.Count == 0)
                return "Hoy todavía **no se registraron visitas** familiares.";
            return $"Hoy hubo **{hoy.Count} visitas** familiares:\n" + string.Join("\n", hoy.Take(10).Select(v =>
                $"- {v.nombreVisitante} ({v.familiar}) visitó a [{($"{v.nombreBebe} {v.apellidoBebe}").Trim()}](bebe:{v.idBebe})"));
        }

        private string? BloqueMamas()
        {
            var madres = negMadres.listarMadres() ?? new List<MADRE>();
            var localidades = madres.GroupBy(m => m.LocalidadDetalle?.nombre ?? "Sin localidad")
                .OrderByDescending(g => g.Count()).Take(5)
                .Select(g => $"- {g.Key}: {g.Count()}");
            return $"Hay **{madres.Count} mamás** registradas. Localidades más frecuentes:\n" + string.Join("\n", localidades);
        }

        private string? BloqueDiaDeHoy()
        {
            var d = negDashboard.ObtenerCoordinacionHoy();
            return $"**Hoy {d.Fecha:dd/MM}** en Resimamis:\n" +
                   $"- Bebés activos: **{d.BebesActivos}** (disponibles para abrazo: {d.BebesDisponiblesAbrazo}, asignados: {d.BebesAsignados})\n" +
                   $"- Abrazos: **{d.AbrazosHoy.Finalizados} finalizados**, {d.AbrazosHoy.EnCurso} en curso, {d.AbrazosHoy.Creados} asignaciones creadas\n" +
                   $"- Voluntarias que ficharon: **{d.VoluntariasConAsistenciaHoy}**\n" +
                   $"- Visitas: **{d.VisitasHoy}**" +
                   (d.AbrazosColgados > 0 ? $"\n- Abrazos sin cerrar: **{d.AbrazosColgados}**" : "");
        }

        // ---------- Opciones fijas tolerantes ----------

        /// <summary>
        /// Elige la opción válida más parecida a lo que mandó el modelo: "sala" → "salas",
        /// "Proveedor" → "proveedores", "estados bebe" → "estados_bebes", "visita" → "visitas".
        /// Null si nada se parece.
        /// </summary>
        private static string? ElegirOpcion(string? valor, params string[] opciones)
        {
            var v = SinAcentos(valor ?? "").Trim().Replace(' ', '_').Replace('-', '_');
            if (v.Length == 0) return null;
            string Norm(string o) => SinAcentos(o).Replace(' ', '_');
            var exacta = opciones.FirstOrDefault(o => Norm(o) == v);
            if (exacta != null) return exacta;
            var sinPlural = string.Join('_', v.Split('_').Select(RaizSingular));
            var porRaiz = opciones.FirstOrDefault(o =>
                string.Join('_', Norm(o).Split('_').Select(RaizSingular)) == sinPlural);
            if (porRaiz != null) return porRaiz;
            var porPrefijo = opciones.FirstOrDefault(o => v.Length >= 4 && (Norm(o).StartsWith(sinPlural) || sinPlural.StartsWith(RaizSingular(Norm(o)))));
            if (porPrefijo != null) return porPrefijo;
            return opciones.FirstOrDefault(o => Parecidas(sinPlural, RaizSingular(Norm(o))));
        }

        // ---------- Búsqueda de personas tolerante ----------

        /// <summary>
        /// "paula martinez" encuentra a "Paula Martínez", "mía acosta" a "Mia Acosta", "martines" a
        /// "Martínez": sin acentos, todas las palabras en cualquier orden y con 1-2 letras de error.
        /// </summary>
        private static bool CoincidePersona(string? nombre, string? apellido, string busqueda)
        {
            var completo = SinAcentos($"{nombre} {apellido}");
            var q = SinAcentos(busqueda).Trim();
            if (q.Length == 0) return false;
            if (completo.Contains(q)) return true;

            var palabras = completo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokens = q.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2)
                .ToList();
            return tokens.Count > 0 && tokens.All(t =>
                palabras.Any(p => p.StartsWith(t) || Parecidas(t, p)));
        }

        private object RankingBebesAbrazos(JsonDocument args)
        {
            var desde = Desde(args, 364);
            var hasta = Hasta(args, 364);
            var top = Math.Clamp(LeerInt(args, "top") ?? 10, 1, 30);
            var desdeUtc = DateTime.SpecifyKind(desde.Date.AddHours(3), DateTimeKind.Utc);
            var hastaUtc = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddHours(3), DateTimeKind.Utc);

            var conteos = db.ASIGNACION
                .Where(a => a.idBebe != null && a.fechaHoraFin != null && !a.cierreAutomatico
                            && a.fechaHoraFin >= desdeUtc && a.fechaHoraFin < hastaUtc)
                .GroupBy(a => a.idBebe!.Value)
                .Select(g => new { idBebe = g.Key, abrazos = g.Count() })
                .OrderByDescending(x => x.abrazos)
                .Take(top)
                .ToList();
            var ids = conteos.Select(c => c.idBebe).ToList();
            var bebes = db.BEBE.Where(b => ids.Contains(b.ID))
                .Select(b => new { b.ID, b.nombre, b.apellido, b.FechaSalida })
                .ToList()
                .ToDictionary(b => b.ID);

            return new
            {
                criterio = "ranking_bebes_por_abrazos_finalizados",
                aclaracion = "Abrazos finalizados reales (sin cierres automáticos) por bebé. Incluye bebés ya egresados (egresado=true).",
                fechaInicio = DateOnly.FromDateTime(desde),
                fechaFin = DateOnly.FromDateTime(hasta),
                ranking = conteos.Select((c, i) => new
                {
                    posicion = i + 1,
                    c.idBebe,
                    nombre = bebes.TryGetValue(c.idBebe, out var b) ? $"{b.nombre} {b.apellido}".Trim() : $"Bebé {c.idBebe}",
                    egresado = bebes.TryGetValue(c.idBebe, out var b2) && b2.FechaSalida != null,
                    c.abrazos
                }).ToList()
            };
        }

        // ---------- Conteos generales (tool + contexto del prompt) ----------

        /// <summary>
        /// Totales de todo el sistema en una sola consulta. Responde cualquier "¿cuántos X hay?" aunque
        /// el modelo no elija bien la herramienta. Cada conteo está aislado: si uno falla, sale null.
        /// </summary>
        private object ConteosGenerales()
        {
            T? Seguro<T>(Func<T> f)
            {
                try { return f(); }
                catch (Exception ex) { Console.WriteLine($"[asistente] conteo falló: {ex.Message}"); return default; }
            }

            var voluntarias = Seguro(() => negVoluntaria.listarVoluntarias()) ?? new List<VOLUNTARIA>();
            var insumos = Seguro(() => negInsumos.obtenerInsumos()) ?? new List<INSUMO>();
            var porSala = Seguro(() => negDashboard.ObtenerBebesPorSala());
            var porEstado = Seguro(() => negDashboard.ObtenerBebesPorEstado());
            var hoyAr = HoyArgentina();
            var inicioMes = new DateTime(hoyAr.Year, hoyAr.Month, 1);
            return new
            {
                criterio = "conteos_generales",
                aclaracion = "Totales actuales de Resimamis. bebesActivos = internados hoy (sin egreso). Las coordinadoras son voluntarias con rol Coordinadora.",
                bebesActivos = porSala?.TotalBebes ?? Seguro(() => negBebes.listarBebes()?.Count),
                bebesPorSala = porSala?.PorSala.Select(s => new { sala = s.NombreSala, cantidad = s.CantidadBebes }),
                bebesHistoricosPorEstado = porEstado?.PorEstado.Select(e => new { estado = e.EstadoBebe, cantidad = e.Cantidad }),
                bebesHistoricosTotal = porEstado?.TotalBebes,
                bebesIngresadosEsteMes = Seguro(() => db.BEBE.Count(b => b.FechaIngresoNEO >= inicioMes)),
                bebesEgresadosEsteMes = Seguro(() => db.BEBE.Count(b => b.FechaSalida >= inicioMes)),
                mesActual = inicioMes.ToString("yyyy-MM"),
                voluntariasTotal = voluntarias.Count,
                voluntariasPorRol = voluntarias
                    .GroupBy(v => string.IsNullOrWhiteSpace(v.rol) ? "Sin rol" : v.rol!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                mamasTotal = Seguro(() => negMadres.listarMadres()?.Count),
                insumosCargados = insumos.Count,
                insumosBajoMinimo = insumos.Count(i => i.stockActual <= i.stockMinimo),
                unidadesEnDeposito = insumos.Sum(i => i.stockActual),
                salasActivas = Seguro(() => negSalas.listarSalasActivas()?.Count),
                hoy = Seguro(() =>
                {
                    var d = negDashboard.ObtenerCoordinacionHoy();
                    return d == null ? null : new
                    {
                        fecha = d.Fecha.ToString("yyyy-MM-dd"),
                        voluntariasQueFicharonHoy = d.VoluntariasConAsistenciaHoy,
                        abrazosFinalizadosHoy = d.AbrazosHoy?.Finalizados,
                        abrazosEnCursoHoy = d.AbrazosHoy?.EnCurso,
                        asignacionesCreadasHoy = d.AbrazosHoy?.Creados,
                        visitasHoy = d.VisitasHoy,
                        bebesDisponiblesParaAbrazo = d.BebesDisponiblesAbrazo,
                    };
                }),
            };
        }

        /// <summary>Resumen corto para el prompt; si la base falla, el prompt sigue sin él.</summary>
        private string ConteosParaPrompt()
        {
            try
            {
                return Json(ConteosGenerales());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[asistente] no se pudieron armar los conteos del prompt: {ex.Message}");
                return "(no disponibles: usá las herramientas)";
            }
        }

        // ---------- Detector de respuestas que se rinden ----------

        private static readonly System.Text.RegularExpressions.Regex RegexRendicion = new(
            @"no (puedo|pude|tengo acceso|tengo (esa|ese|la|el) (informaci|dato)|cuento con|es posible|logr[eo]|entiendo|entend[ií]|s[eé] (a qu[eé]|qu[eé])|hay (una )?herramienta)|" +
            @"parece que (has escrito|escribiste)|no est[aá] (claro|disponible)|podr[ií]as (reformular|aclarar|especificar)|no comprendo|sin un criterio",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private static readonly System.Text.RegularExpressions.Regex RegexSaludo = new(
            @"^\s*(hola|buen[oa]s?( d[ií]as| tardes| noches)?|gracias|muchas gracias|ok|okey|dale|chau|adi[oó]s|genial|perfecto|listo)[\s!.,?¡¿]*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// <summary>
        /// El modelo contestó sin consultar nada y con un "no puedo / no entiendo": hay que insistir
        /// obligándolo a usar una herramienta. Saludos sueltos no cuentan.
        /// </summary>
        private static bool SeRindio(string texto, string pregunta) =>
            !RegexSaludo.IsMatch(pregunta) && RegexRendicion.IsMatch(texto);

        // ---------- Búsqueda de insumos tolerante ----------

        /// <summary>
        /// Insumos cuyo nombre coincide con alguna palabra del texto, sin acentos, sin plural y con
        /// hasta 1-2 letras de error ("chupte", "chupetes", "panales" → Chupete, Pañales).
        /// soloNombres=true ignora la descripción (para detectar si una pregunta suelta nombra un insumo).
        /// </summary>
        private List<INSUMO> InsumosQueCoinciden(string? texto, bool soloNombres = false)
        {
            var todos = negInsumos.obtenerInsumos() ?? new List<INSUMO>();
            var palabras = SinAcentos(texto ?? "")
                .Split(new[] { ' ', ',', '.', '-', '/', '?', '¿', '!', '¡', ':', ';', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 3 && !PalabrasVacias.Contains(p))
                .Select(RaizSingular)
                .Where(r => r.Length >= 3)
                .Distinct()
                .ToList();
            if (palabras.Count == 0 || todos.Count == 0)
                return new List<INSUMO>();

            var puntuados = todos.Select(i =>
            {
                var fuente = SinAcentos(soloNombres ? i.nombre ?? "" : $"{i.nombre} {i.descripcion}");
                var completas = fuente
                    .Split(new[] { ' ', ',', '.', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(r => r.Length >= 3)
                    .ToList();
                // Se compara contra la palabra completa y contra su raíz singular.
                var palabrasInsumo = completas.Concat(completas.Select(RaizSingular)).Distinct().ToList();
                var aciertos = palabras.Count(p =>
                    fuente.Contains(p) || palabrasInsumo.Any(w => w.StartsWith(p) || p.StartsWith(w) && w.Length >= 4 || Parecidas(p, w)));
                return (insumo: i, aciertos);
            })
            .Where(x => x.aciertos > 0)
            .ToList();

            if (puntuados.Count == 0)
                return new List<INSUMO>();
            var mejor = puntuados.Max(x => x.aciertos);
            return puntuados.Where(x => x.aciertos == mejor)
                .Select(x => x.insumo)
                .OrderBy(i => i.nombre)
                .ToList();
        }

        /// <summary>Errores de tipeo: 1 letra en palabras cortas, 2 en palabras de 7 o más.</summary>
        private static bool Parecidas(string a, string b)
        {
            if (a.Length < 4 || b.Length < 4) return false;
            // Suenan igual: "xupete" / "chpte" / "chupete", "toayitas" / "toallitas", "jeringa" / "geringa".
            var ea = Esqueleto(a);
            if (ea.Length >= 3 && ea == Esqueleto(b)) return true;
            var tolerancia = Math.Max(a.Length, b.Length) >= 7 ? 2 : 1;
            if (Math.Abs(a.Length - b.Length) > tolerancia) return false;
            return Levenshtein(a, b) <= tolerancia;
        }

        /// <summary>
        /// Esqueleto fonético en castellano: equivalencias de sonido (ll/y, ch/x, z/s, v/b, qu/k/c, g/j),
        /// sin vocales ni h, sin letras repetidas y sin la s final del plural.
        /// </summary>
        private static string Esqueleto(string palabra)
        {
            var w = SinAcentos(palabra)
                .Replace("ll", "y").Replace("ch", "c").Replace("qu", "c").Replace("sh", "c")
                .Replace('x', 'c').Replace('k', 'c').Replace('z', 's').Replace('v', 'b').Replace('w', 'b')
                .Replace("ge", "je").Replace("gi", "ji").Replace("ce", "se").Replace("ci", "si");
            var sb = new StringBuilder();
            foreach (var c in w)
            {
                if ("aeiouh".Contains(c) || !char.IsLetter(c)) continue;
                if (sb.Length > 0 && sb[^1] == c) continue;
                sb.Append(c);
            }
            if (sb.Length > 3 && sb[^1] == 's') sb.Length--;
            return sb.ToString();
        }

        /// <summary>Distancia de edición contando como 1 error dos letras invertidas ("chupte" ↔ "chupet").</summary>
        private static int Levenshtein(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (var j = 0; j <= b.Length; j++) d[0, j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + costo);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }
            return d[a.Length, b.Length];
        }
    }
}
