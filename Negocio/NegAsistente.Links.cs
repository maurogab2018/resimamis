using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Verificador de links a personas: "[Bebé 1](bebe:1)" con un nombre que no es el de ese id
    /// es un nombre inventado (pasó con "¿cómo se llaman los bebés que ingresaron?").
    /// Cada link se compara con la persona real de la base; si no coincide, se le exige al modelo
    /// consultar y, si insiste, el link se saca y se marca como no verificado.
    /// </summary>
    public partial class NegAsistente
    {
        private static readonly Regex RegexLinkPersona = new(@"\[([^\]\n]+)\]\((bebe|voluntaria|madre):(\d+)\)", RegexOptions.Compiled);

        private const string MarcaSinVerificar = " (sin verificar)";

        /// <summary>Links cuyo nombre no corresponde a la persona real con ese id (o cuyo id no existe).</summary>
        private List<string> LinksQueNoCoinciden(string texto)
        {
            var links = RegexLinkPersona.Matches(texto).Cast<Match>().ToList();
            if (links.Count == 0)
                return new List<string>();

            Dictionary<(string, int), string> reales;
            try
            {
                reales = NombresReales(links.Select(m => (m.Groups[2].Value, int.Parse(m.Groups[3].Value))).Distinct().ToList());
            }
            catch (Exception ex)
            {
                // Si la base no responde, no se frena la respuesta por esto.
                Console.WriteLine($"[asistente] no se pudieron verificar los links: {ex.Message}");
                return new List<string>();
            }

            return links
                .Where(m =>
                {
                    var clave = (m.Groups[2].Value, int.Parse(m.Groups[3].Value));
                    return !reales.TryGetValue(clave, out var real) || !NombreCoincide(m.Groups[1].Value, real);
                })
                .Select(m => m.Value)
                .Distinct()
                .ToList();
        }

        private Dictionary<(string, int), string> NombresReales(List<(string Tipo, int Id)> claves)
        {
            var reales = new Dictionary<(string, int), string>();

            // Primero lo que ya devolvieron las herramientas en esta pregunta.
            foreach (var p in personasVistas.Values.Where(p => p != null).Cast<Persona>())
                reales[(p.Tipo, p.Id)] = p.Nombre;

            var faltan = claves.Where(c => !reales.ContainsKey(c)).ToList();
            var idsBebe = faltan.Where(c => c.Tipo == "bebe").Select(c => c.Id).ToList();
            var idsVol = faltan.Where(c => c.Tipo == "voluntaria").Select(c => c.Id).ToList();
            var idsMadre = faltan.Where(c => c.Tipo == "madre").Select(c => c.Id).ToList();

            if (idsBebe.Count > 0)
                foreach (var b in db.BEBE.Where(b => idsBebe.Contains(b.ID)).Select(b => new { b.ID, b.nombre, b.apellido }).ToList())
                    reales[("bebe", b.ID)] = $"{b.nombre} {b.apellido}".Trim();
            if (idsVol.Count > 0)
                foreach (var v in db.VOLUNTARIA.Where(v => idsVol.Contains(v.IdVoluntaria)).Select(v => new { v.IdVoluntaria, v.Nombre, v.Apellido }).ToList())
                    reales[("voluntaria", v.IdVoluntaria)] = $"{v.Nombre} {v.Apellido}".Trim();
            if (idsMadre.Count > 0)
                foreach (var m in db.MADRE.Where(m => idsMadre.Contains(m.IdMadre)).Select(m => new { m.IdMadre, m.Nombre, m.Apellido }).ToList())
                    reales[("madre", m.IdMadre)] = $"{m.Nombre} {m.Apellido}".Trim();

            return reales;
        }

        /// <summary>"Mia", "Mia Acosta" o "Acosta" coinciden con "Mia Acosta"; "Bebé 1" no.</summary>
        private static bool NombreCoincide(string enLink, string real)
        {
            var palabrasReales = SinAcentos(real).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var palabrasLink = SinAcentos(enLink)
                .Split(new[] { ' ', '*', '_', '.', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 2)
                .ToList();
            return palabrasLink.Count > 0 && palabrasLink.All(palabrasReales.Contains);
        }

        /// <summary>Último recurso: el link inventado se convierte en texto marcado como no verificado.</summary>
        private string QuitarLinksInvalidos(string texto)
        {
            var malos = LinksQueNoCoinciden(texto);
            if (malos.Count == 0)
                return texto;
            foreach (var malo in malos)
            {
                var nombre = RegexLinkPersona.Match(malo).Groups[1].Value;
                texto = texto.Replace(malo, nombre + MarcaSinVerificar);
            }
            return texto;
        }
    }
}
