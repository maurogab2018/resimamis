using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Guardas para no confundir personas:
    /// 1) Antes de cada herramienta, un nombre (nombre_bebe / nombre_voluntaria / nombre_madre) se
    ///    resuelve a id del lado del servidor; si el id que mandó el modelo no es de esa persona, gana el nombre.
    /// 2) Después de cada herramienta se anotan las personas que aparecieron (id + nombre) y la respuesta
    ///    final las linkea ([Mia Acosta](bebe:16)) aunque el modelo no lo haga. Así el id viaja en el
    ///    historial y un seguimiento ("peso", "¿y la mamá?") no termina mostrando datos de otro bebé.
    /// </summary>
    public partial class NegAsistente
    {
        private sealed record Persona(string Tipo, int Id, string Nombre);

        /// <summary>Personas vistas en las herramientas de esta pregunta (clave: tipo + nombre sin acentos).</summary>
        private readonly Dictionary<string, Persona?> personasVistas = new();

        private static readonly Dictionary<string, (string Tipo, string IdKey, string NombreKey)> HerramientasDePersona = new()
        {
            ["ficha_bebe"] = ("bebe", "id_bebe", "nombre_bebe"),
            ["abrazos_bebe"] = ("bebe", "id_bebe", "nombre_bebe"),
            ["visitas_bebe"] = ("bebe", "id_bebe", "nombre_bebe"),
            ["ficha_voluntaria"] = ("voluntaria", "id_voluntaria", "nombre_voluntaria"),
            ["abrazos_voluntaria"] = ("voluntaria", "id_voluntaria", "nombre_voluntaria"),
            ["asignaciones_hoy_voluntaria"] = ("voluntaria", "id_voluntaria", "nombre_voluntaria"),
            ["dias_turnos_voluntarias"] = ("voluntaria", "id_voluntaria", "nombre_voluntaria"),
            ["asistencias_periodo"] = ("voluntaria", "id_voluntaria", "nombre_voluntaria"),
            ["ficha_madre"] = ("madre", "id_madre", "nombre_madre"),
        };

        /// <summary>
        /// Devuelve los argumentos con el id correcto, o un JSON de error (para devolverle al modelo)
        /// cuando el nombre no existe o es ambiguo.
        /// </summary>
        private (string? Argumentos, string? Error) ResolverPersonaEnArgumentos(string herramienta, string? argumentosJson)
        {
            if (!HerramientasDePersona.TryGetValue(herramienta, out var def))
                return (argumentosJson, null);

            JsonObject obj;
            try
            {
                obj = JsonNode.Parse(string.IsNullOrWhiteSpace(argumentosJson) ? "{}" : argumentosJson) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                return (argumentosJson, null);
            }

            var camel = def.IdKey == "id_bebe" ? "idBebe" : def.IdKey == "id_madre" ? "idMadre" : "idVoluntaria";
            int? id = null;
            foreach (var k in new[] { def.IdKey, camel })
            {
                if (obj[k] is JsonValue v)
                {
                    if (v.TryGetValue<int>(out var n)) id = n;
                    else if (v.TryGetValue<string>(out var s) && int.TryParse(s, out var n2)) id = n2;
                }
            }
            var nombre = (obj[def.NombreKey] ?? obj["nombre"] ?? obj["texto"])?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
                return (argumentosJson, null);

            var candidatos = BuscarPersonas(def.Tipo, nombre);
            if (candidatos.Count == 0)
            {
                // Aunque venga un id, no se usa: ese id es de OTRA persona (es justo el caso "peso" → datos de otro bebé).
                return (null, Json(new
                {
                    error = $"No hay ningún registro de {Etiqueta(def.Tipo)} que se llame '{nombre}'. Decíselo a la coordinadora; NO muestres datos de otra persona.",
                }));
            }

            // Si el id que mandó el modelo es uno de los candidatos, se respeta; si no, gana el nombre.
            Persona? elegido = id is > 0 ? candidatos.FirstOrDefault(c => c.Id == id) : null;
            if (elegido == null)
            {
                var exactos = candidatos.Where(c => SinAcentos(c.Nombre) == SinAcentos(nombre)).ToList();
                if (exactos.Count == 1) elegido = exactos[0];
                else if (candidatos.Count == 1) elegido = candidatos[0];
            }

            if (elegido == null)
            {
                return (null, Json(new
                {
                    error = $"Hay varios registros de {Etiqueta(def.Tipo)} que coinciden con '{nombre}'. Mostrale las opciones a la coordinadora y preguntale cuál es.",
                    opciones = candidatos.Take(10).Select(c => new { id = c.Id, nombre = c.Nombre })
                }));
            }

            if (id is > 0 && id != elegido.Id)
                Console.WriteLine($"[asistente] {herramienta}: el modelo mandó id {id} pero pidió '{nombre}' → uso id {elegido.Id}.");
            obj[def.IdKey] = elegido.Id;
            obj.Remove(camel);
            return (obj.ToJsonString(), null);
        }

        private static string Etiqueta(string tipo) => tipo switch
        {
            "bebe" => "bebé",
            "madre" => "mamá",
            _ => "voluntaria",
        };

        private List<Persona> BuscarPersonas(string tipo, string nombre)
        {
            switch (tipo)
            {
                case "bebe":
                    // Incluye egresados: también se pregunta por bebés que ya se fueron de alta.
                    return db.BEBE.AsNoTracking()
                        .Select(b => new { b.ID, b.nombre, b.apellido, b.FechaSalida })
                        .ToList()
                        .Where(b => CoincidePersona(b.nombre, b.apellido, nombre))
                        .OrderBy(b => b.FechaSalida.HasValue)
                        .Select(b => new Persona("bebe", b.ID, $"{b.nombre} {b.apellido}".Trim()))
                        .ToList();
                case "madre":
                    return (negMadres.listarMadres() ?? new List<MADRE>())
                        .Where(m => CoincidePersona(m.Nombre, m.Apellido, nombre))
                        .Select(m => new Persona("madre", m.IdMadre, $"{m.Nombre} {m.Apellido}".Trim()))
                        .ToList();
                default:
                    return (negVoluntaria.listarVoluntarias() ?? new List<VOLUNTARIA>())
                        .Where(v => CoincidePersona(v.Nombre, v.Apellido, nombre))
                        .Select(v => new Persona("voluntaria", v.IdVoluntaria, $"{v.Nombre} {v.Apellido}".Trim()))
                        .ToList();
            }
        }

        /// <summary>Un nombre suelto ("lucas", "gonzalez", "paula martinez") se busca en bebés, voluntarias y mamás a la vez.</summary>
        private object BuscarPersona(string? texto)
        {
            texto = texto?.Trim() ?? "";
            if (texto.Length == 0)
                return new { error = "Indicá un nombre o apellido." };

            object Seguro(Func<object> f)
            {
                try { return f(); }
                catch (Exception ex) { return new List<object> { new { error = ex.Message } }; }
            }

            return new
            {
                criterio = "buscar_persona",
                aclaracion = "Coincidencias en las tres listas (sin acentos, tolera errores). Si hay más de una persona posible, mostralas todas con su tipo y preguntá cuál.",
                bebes = Seguro(() => BuscarBebes(texto)),
                voluntarias = Seguro(() => BuscarVoluntarias(texto)),
                madres = Seguro(() => BuscarMadres(texto)),
            };
        }

        /// <summary>Mamás filtradas por localidad y/o nombre (tolera acentos y errores: "rio cuarto", "cordova").</summary>
        private object ListarMadres(JsonDocument args)
        {
            var localidad = (LeerString(args, "localidad") ?? LeerString(args, "filtro_localidad") ?? "").Trim();
            var texto = (LeerString(args, "texto") ?? "").Trim();
            var todas = negMadres.listarMadres() ?? new List<MADRE>();
            IEnumerable<MADRE> q = todas;
            string? localidadUsada = null;
            if (localidad.Length > 0)
            {
                var objetivo = SinAcentos(localidad);
                var porContiene = todas.Where(m => SinAcentos(m.LocalidadDetalle?.nombre ?? "").Contains(objetivo)).ToList();
                if (porContiene.Count > 0)
                {
                    q = porContiene;
                    localidadUsada = localidad;
                }
                else
                {
                    var nombres = todas.Select(m => m.LocalidadDetalle?.nombre).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToArray();
                    localidadUsada = ElegirOpcion(localidad, nombres!);
                    q = localidadUsada == null ? Enumerable.Empty<MADRE>() : todas.Where(m => m.LocalidadDetalle?.nombre == localidadUsada);
                }
            }
            if (texto.Length > 0)
                q = q.Where(m => CoincidePersona(m.Nombre, m.Apellido, texto));
            var minHijos = LeerInt(args, "min_hijos");
            if (minHijos is > 0)
                q = q.Where(m => m.CantidadHijos >= minHijos);

            var lista = q.OrderByDescending(m => m.CantidadHijos).ToList();
            return new
            {
                criterio = "listar_madres",
                filtroLocalidad = localidadUsada ?? (localidad.Length > 0 ? localidad : null),
                aclaracion = localidad.Length > 0 && localidadUsada == null
                    ? $"No hay mamás de '{localidad}'. Mostrá las localidades disponibles."
                    : "'total' es la cantidad real; la lista muestra hasta 40.",
                localidadesDisponibles = localidad.Length > 0 && localidadUsada == null
                    ? todas.GroupBy(m => m.LocalidadDetalle?.nombre ?? "Sin localidad").Select(g => new { localidad = g.Key, cantidad = g.Count() })
                    : null,
                total = lista.Count,
                datosDeContacto = "Las listas no traen DNI ni teléfono: esos datos se dan solo de UNA mamá puntual (ficha_madre).",
                madres = lista.Take(MaxItemsLista).Select(m => new { idMadre = m.IdMadre, nombre = m.Nombre, apellido = m.Apellido, localidad = m.LocalidadDetalle?.nombre, cantidadHijos = m.CantidadHijos }).ToList()
            };
        }

        // ---------- Intención de la pregunta (médica, juicio, escritura, contacto masivo) ----------

        private static readonly Regex RegexMedicaFuerte = new(
            @"\b(morir\w*|muere|muera|mueren|medicaci\w*|medicament\w*|remedio\w*|fiebre|dosis|pronostic\w*|sobreviv\w*|grave|gravedad|curar\w*|tratamiento\w*)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexMedicaConBebe = new(@"\b(riesgo|salud|peligro)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexJuicio = new(@"\b(peor|peores|mejor|mejores|buena|bueno|mala|malo|vag[ao]s?|inutil\w*|confiable)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexPersona = new(@"\b(voluntari\w*|mama\w*|madre\w*|coordinador\w*|persona\w*|chica\w*)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexEscritura = new(
            @"\b(borr[ae]|borrar|borren|borrame|elimin[ae]|eliminar|eliminame|modific[ae]|modificar|actualiz[ae]|actualizar|dar de (alta|baja)|dale de (alta|baja)|da de (alta|baja)|resetea|resetear|vacia|vaciar)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexContacto = new(@"\b(dni|telefono\w*|celular\w*|direccion\w*|domicilio\w*|contacto\w*|mail\w*)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexMasivo = new(@"\b(todas|todos|lista|listado|completo|cada una)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public const string AvisoMedico = "No doy información médica ni pronósticos: eso lo responde el equipo médico de neonatología.";
        public const string AvisoJuicio = "El sistema no evalúa a las personas; lo que sí puedo mostrarte son datos de actividad.";

        private static bool EsPreguntaMedica(string pregunta)
        {
            var p = SinAcentos(pregunta);
            return RegexMedicaFuerte.IsMatch(p) || (RegexMedicaConBebe.IsMatch(p) && p.Contains("beb"));
        }

        private static bool EsJuicioSobrePersona(string pregunta)
        {
            var p = SinAcentos(pregunta);
            return RegexJuicio.IsMatch(p) && RegexPersona.IsMatch(p);
        }

        private static bool PideEscritura(string pregunta) => RegexEscritura.IsMatch(SinAcentos(pregunta));

        private static bool PideContactoMasivo(string pregunta)
        {
            var p = SinAcentos(pregunta);
            return RegexContacto.IsMatch(p) && RegexMasivo.IsMatch(p);
        }

        /// <summary>Notas para el modelo según lo que se detecta en la pregunta (null si no hay nada especial).</summary>
        private static string? NotaDeIntencion(string pregunta)
        {
            var notas = new List<string>();
            if (EsPreguntaMedica(pregunta))
                notas.Add($"Es una pregunta médica o de pronóstico. Empezá con: \"{AvisoMedico}\" Después, si nombran a un bebé, ofrecé sus datos de Resimamis (sala, peso, abrazos). No vuelques conteos generales.");
            if (EsJuicioSobrePersona(pregunta))
                notas.Add($"Pide calificar a una persona. Empezá con: \"{AvisoJuicio}\" Si sirve, mostrá datos de actividad (ranking_abrazos_voluntarias, con orden='menos' si piden 'la peor' o 'la que menos').");
            if (PideEscritura(pregunta))
                notas.Add("Si te piden borrar, eliminar, modificar o dar de alta/baja algo: decí que el asistente solo consulta y que eso se hace desde la pantalla correspondiente. No muestres datos que no pidieron. Si preguntan CÓMO se hace, explicalo con guia_operativa. (Si no es un pedido de modificación, ignorá esta nota.)");
            if (PideContactoMasivo(pregunta))
                notas.Add("Pide datos de contacto de muchas personas: por privacidad no se listan juntos. Ofrecé buscar a UNA persona puntual.");
            return notas.Count == 0 ? null : "Nota del sistema sobre esta pregunta: " + string.Join(" ", notas);
        }

        /// <summary>Si el modelo ignoró la nota, se agrega el aviso igual (médico y juicio son los que no pueden faltar).</summary>
        private static string AjustarPorIntencion(string pregunta, string texto)
        {
            var t = SinAcentos(texto);
            if (EsJuicioSobrePersona(pregunta) && !Regex.IsMatch(t, @"no (se )?(evalua|califica|juzga|puntua)", RegexOptions.IgnoreCase))
                texto = AvisoJuicio + "\n\n" + texto;
            if (EsPreguntaMedica(pregunta) && !t.Contains("medic"))
                texto = AvisoMedico + "\n\n" + texto;
            return texto;
        }

        // ---------- Afirmaciones que el sistema no registra ----------

        /// <summary>Solo lo que devolvieron las herramientas (sin la pregunta: la pregunta puede traer una premisa falsa).</summary>
        private readonly System.Text.StringBuilder evidenciaHerramientas = new();

        /// <summary>Muerte, enfermedad o diagnóstico: solo vale si lo trajo una herramienta (ficha_bebe trae el diagnóstico de ingreso/egreso) (una baja no dice el motivo).</summary>
        private static readonly Regex RegexAfirmacionSensible = new(
            @"\b(fallec\w*|muert[oae]s?|muri[óo]|murieron|defunci\w*|[óo]bitos?|enferm[oa]s?|enfermedad\w*|diagn[óo]stic\w*)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal const string AclaracionSinRegistro =
            "Aclaración: Resimamis no registra fallecimientos, enfermedades ni pronósticos (solo la ficha de cada bebé puede tener su diagnóstico de ingreso o egreso). Que una persona esté dada de baja solo significa que ya no está activa en el sistema; no indica el motivo.";

        /// <summary>
        /// Palabras de la respuesta sobre muerte/enfermedad que no salen de ninguna herramienta
        /// (ej. la coordinadora preguntó "madres muertas" y el modelo afirmó que "todas fallecieron").
        /// </summary>
        private List<string> AfirmacionesSinRespaldo(string respuesta)
        {
            var fuente = evidenciaHerramientas.ToString();
            return RegexAfirmacionSensible.Matches(respuesta)
                .Select(m => m.Value)
                .Where(p => !fuente.Contains(p[..Math.Min(p.Length, 6)], StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Si después de corregir todavía habla de eso, se agrega la aclaración (salvo que ya la diga).</summary>
        private string ConAclaracionSiHaceFalta(string texto)
        {
            if (AfirmacionesSinRespaldo(texto).Count == 0)
                return texto;
            if (AclaraQueNoSeRegistra(texto))
                return texto;
            return texto.TrimEnd() + "\n\n" + AclaracionSinRegistro;
        }

        /// <summary>La respuesta ya explica que ese dato no existe ("el sistema no registra fallecimientos").</summary>
        private static bool AclaraQueNoSeRegistra(string texto) =>
            Regex.IsMatch(texto, @"\bno (se )?(registra|guarda|tiene registr|lleva registr|figura)", RegexOptions.IgnoreCase);

        /// <summary>Habla de muertes/enfermedades sin respaldo y sin aclarar que el sistema no lo guarda.</summary>
        private List<string> PremisaSinRespaldo(string texto) =>
            AclaraQueNoSeRegistra(texto) ? new List<string>() : AfirmacionesSinRespaldo(texto);

        // ---------- Verificador de números ----------

        /// <summary>Todo lo que el modelo pudo ver en esta pregunta: Datos rápidos, historial y resultados de herramientas.</summary>
        private readonly System.Text.StringBuilder evidencia = new();

        private static readonly Regex RegexNumero = new(@"(?<![\w:/-])\d{1,3}(?:[.,]\d{3})+(?![\w])|(?<![\w:/.,-])\d+(?:[.,]\d+)?(?![\w/])", RegexOptions.Compiled);
        private static readonly Regex RegexFechaOLink = new(@"\]\((bebe|voluntaria|madre):\d+\)|\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}(/\d{2,4})?|\d{1,2}:\d{2}|^\s*\d+\.\s", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Números de la respuesta que no aparecen en ninguna evidencia (ej. "Gasas: 68 (mínimo 100)" inventado).
        /// Se ignoran fechas, horas, ids de links, numeración de listas, años y números chicos (≤ 10).
        /// </summary>
        private List<string> NumerosSinRespaldo(string respuesta)
        {
            var limpio = RegexFechaOLink.Replace(respuesta, " ");
            var fuente = evidencia.ToString();
            var faltan = new List<string>();
            var respaldados = new List<decimal>();
            var dudosos = new List<(string crudo, decimal valor)>();
            foreach (Match m in RegexNumero.Matches(limpio))
            {
                var crudo = m.Value;
                var sinMiles = Regex.IsMatch(crudo, @"^\d{1,3}([.,]\d{3})+$") ? Regex.Replace(crudo, "[.,]", "") : crudo;
                if (!decimal.TryParse(sinMiles.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valor))
                    continue;
                if (valor <= 10 || (valor >= 1990 && valor <= 2100 && valor == Math.Floor(valor)))
                {
                    respaldados.Add(valor);
                    continue;
                }
                var entero = Math.Floor(valor).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (fuente.Contains(sinMiles) || fuente.Contains(sinMiles.Replace(',', '.')) || Regex.IsMatch(fuente, $@"(?<!\d){Regex.Escape(entero)}(?!\d)"))
                {
                    respaldados.Add(valor);
                    continue;
                }
                dudosos.Add((crudo, valor));
            }

            // Una cuenta con datos respaldados de la misma respuesta no es invento: "faltan 276 (300 − 24)".
            foreach (var (crudo, valor) in dudosos)
            {
                var esCuenta = respaldados.Any(a => respaldados.Any(b =>
                    Math.Abs(a - b - valor) < 0.6m || Math.Abs(a + b - valor) < 0.6m));
                if (!esCuenta) faltan.Add(crudo);
            }
            return faltan.Distinct().ToList();
        }

        // ---------- Personas vistas → links automáticos ----------

        private void AnotarPersonas(string resultadoJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(resultadoJson);
                Recorrer(doc.RootElement, 0);
            }
            catch (JsonException)
            {
            }
        }

        private void Recorrer(JsonElement e, int profundidad)
        {
            if (profundidad > 8) return;
            if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var x in e.EnumerateArray()) Recorrer(x, profundidad + 1);
                return;
            }
            if (e.ValueKind != JsonValueKind.Object) return;

            string? Texto(params string[] claves)
            {
                foreach (var c in claves)
                    foreach (var p in e.EnumerateObject())
                        if (string.Equals(p.Name, c, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                            return p.Value.GetString();
                return null;
            }
            int? Numero(string clave)
            {
                foreach (var p in e.EnumerateObject())
                    if (string.Equals(p.Name, clave, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var n))
                        return n;
                return null;
            }
            string NombreCompleto(string? nombre, string? apellido) => $"{nombre} {apellido}".Trim();

            var nom = Texto("nombre");
            var ape = Texto("apellido");
            if (Numero("idBebe") is int idB)
            {
                var n = Texto("nombreBebe") ?? (ape != null ? NombreCompleto(nom, ape) : null);
                if (!string.IsNullOrWhiteSpace(n) && !e.TryGetProperty("idVoluntaria", out _)) Anotar("bebe", idB, n);
                else if (Texto("nombreBebe") is { } nb) Anotar("bebe", idB, nb);
            }
            if (Numero("idVoluntaria") is int idV)
            {
                var n = Texto("nombreVoluntaria") ?? (!e.TryGetProperty("idBebe", out _) ? (ape != null ? NombreCompleto(nom, ape) : nom) : null);
                if (!string.IsNullOrWhiteSpace(n)) Anotar("voluntaria", idV, n);
            }
            if (Numero("idMadre") is int idM && ape != null && !e.TryGetProperty("idBebe", out _))
                Anotar("madre", idM, NombreCompleto(nom, ape));

            foreach (var p in e.EnumerateObject())
                if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    // La madre dentro de la ficha de un bebé es otra persona.
                    if (string.Equals(p.Name, "madre", StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Object
                        && p.Value.TryGetProperty("idMadre", out var im) && im.TryGetInt32(out var idMadre))
                    {
                        var n = NombreCompleto(
                            p.Value.TryGetProperty("nombre", out var pn) ? pn.GetString() : null,
                            p.Value.TryGetProperty("apellido", out var pa) ? pa.GetString() : null);
                        if (!string.IsNullOrWhiteSpace(n)) Anotar("madre", idMadre, n);
                        continue;
                    }
                    Recorrer(p.Value, profundidad + 1);
                }
        }

        private void Anotar(string tipo, int id, string nombre)
        {
            nombre = nombre.Trim();
            if (id <= 0 || nombre.Length < 4 || !nombre.Contains(' ')) return;
            var clave = tipo + "|" + SinAcentos(nombre);
            if (personasVistas.TryGetValue(clave, out var previa))
            {
                // Dos personas distintas con el mismo nombre: no se linkea ninguna.
                if (previa != null && previa.Id != id) personasVistas[clave] = null;
                return;
            }
            personasVistas[clave] = new Persona(tipo, id, nombre);
        }

        /// <summary>
        /// Linkea la primera mención sin link de cada persona vista: "**Mia Acosta**" → "**[Mia Acosta](bebe:16)**".
        /// </summary>
        private string LinkearPersonas(string texto)
        {
            foreach (var p in personasVistas.Values.Where(p => p != null).Cast<Persona>().OrderByDescending(p => p.Nombre.Length))
            {
                if (texto.Contains($"]({p.Tipo}:{p.Id})")) continue;
                var patron = new Regex(@"(?<![\[\p{L}])" + Regex.Escape(p.Nombre) + @"(?![\]\p{L}])", RegexOptions.IgnoreCase);
                var m = patron.Match(texto);
                if (!m.Success) continue;
                texto = texto[..m.Index] + $"[{m.Value}]({p.Tipo}:{p.Id})" + texto[(m.Index + m.Length)..];
            }
            return texto;
        }
    }
}
