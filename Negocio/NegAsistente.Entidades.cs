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
            foreach (Match m in RegexNumero.Matches(limpio))
            {
                var crudo = m.Value;
                var sinMiles = Regex.IsMatch(crudo, @"^\d{1,3}([.,]\d{3})+$") ? Regex.Replace(crudo, "[.,]", "") : crudo;
                if (!decimal.TryParse(sinMiles.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valor))
                    continue;
                if (valor <= 10 || (valor >= 1990 && valor <= 2100 && valor == Math.Floor(valor)))
                    continue;
                var entero = Math.Floor(valor).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (fuente.Contains(sinMiles) || fuente.Contains(sinMiles.Replace(',', '.')) || Regex.IsMatch(fuente, $@"(?<!\d){Regex.Escape(entero)}(?!\d)"))
                    continue;
                faltan.Add(crudo);
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
