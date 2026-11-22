using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// Registro de preguntas y calificación opcional (👍/👎) para encontrar respuestas malas y mejorarlas.
    /// Nunca rompe el chat: si guardar falla, la respuesta sale igual (sin IdRegistro).
    /// Tope para no llenar la base: las 1000 más nuevas y, aparte, las 200 👎 más nuevas.
    /// </summary>
    public partial class NegAsistente
    {
        public const int MaxRegistros = 1000;
        public const int MaxRegistrosMalos = 200;
        private const int MaxRespuestaRegistro = 4000;
        private const int MaxComentario = 300;

        private bool insistioActual;
        private int tokensActual;

        private static string Recortar(string? texto, int max)
        {
            texto ??= "";
            return texto.Length > max ? texto[..max] : texto;
        }

        private int? GuardarRegistro(int dni, string pregunta, AsistentePreguntaRespuesta respuesta, int duracionMs)
        {
            try
            {
                var herramientas = respuesta.HerramientasUsadas ?? new List<string>();
                var resultado = herramientas.Contains("respaldo_datos_sistema", StringComparer.OrdinalIgnoreCase)
                    ? "respaldo"
                    : insistioActual ? "insistio" : "ok";
                var registro = new ASISTENTEREGISTRO
                {
                    fecha = DateTime.UtcNow,
                    dni = dni,
                    pregunta = Recortar(pregunta, MaxPregunta),
                    respuesta = Recortar(respuesta.Respuesta, MaxRespuestaRegistro),
                    herramientas = Recortar(string.Join(", ", herramientas), 500),
                    resultado = resultado,
                    duracionMs = duracionMs,
                    tokens = tokensActual,
                };
                db.ASISTENTEREGISTRO.Add(registro);
                db.SaveChanges();
                PodarRegistros();
                return registro.idAsistenteRegistro;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[asistente] no se pudo guardar el registro: {ex.Message}");
                return null;
            }
        }

        /// <summary>Borra lo que pase del tope. Corre después de cada pregunta, así que casi siempre borra 0 o 1 fila.</summary>
        private void PodarRegistros()
        {
            try
            {
                var viejas = db.ASISTENTEREGISTRO
                    .Where(r => r.calificacion == null || r.calificacion != -1)
                    .OrderByDescending(r => r.idAsistenteRegistro)
                    .Skip(MaxRegistros)
                    .ToList();
                var malasViejas = db.ASISTENTEREGISTRO
                    .Where(r => r.calificacion == -1)
                    .OrderByDescending(r => r.idAsistenteRegistro)
                    .Skip(MaxRegistrosMalos)
                    .ToList();
                if (viejas.Count == 0 && malasViejas.Count == 0)
                    return;
                db.ASISTENTEREGISTRO.RemoveRange(viejas);
                db.ASISTENTEREGISTRO.RemoveRange(malasViejas);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[asistente] no se pudieron borrar registros viejos: {ex.Message}");
            }
        }

        public void Calificar(int dniSolicitante, AsistenteCalificarRequest request)
        {
            if (request == null || request.IdRegistro <= 0)
                throw new ApplicationException("Debe indicar qué respuesta califica.");
            if (request.Calificacion is not (1 or -1 or 0))
                throw new ApplicationException("La calificación debe ser 1 (bien), -1 (mal) o 0 (quitar).");

            var registro = db.ASISTENTEREGISTRO.FirstOrDefault(r =>
                r.idAsistenteRegistro == request.IdRegistro && r.dni == dniSolicitante);
            if (registro == null)
                throw new NotFoundException("No se encontró esa respuesta del asistente.");

            registro.calificacion = request.Calificacion == 0 ? null : (short)request.Calificacion;
            var comentario = request.Comentario?.Trim();
            registro.comentario = request.Calificacion == 0 || string.IsNullOrEmpty(comentario)
                ? null
                : Recortar(comentario, MaxComentario);
            db.SaveChanges();
            if (registro.calificacion == -1)
                PodarRegistros();
        }

        public List<AsistenteRegistroItem> ObtenerRegistros(int dniSolicitante, bool soloMalas, int limite)
        {
            negUsuarios.ValidarCoordinadora(dniSolicitante);
            limite = Math.Clamp(limite <= 0 ? 100 : limite, 1, MaxRegistros + MaxRegistrosMalos);

            var query = db.ASISTENTEREGISTRO.AsNoTracking();
            if (soloMalas)
                query = query.Where(r => r.calificacion == -1 || r.resultado != "ok");

            return query
                .OrderByDescending(r => r.idAsistenteRegistro)
                .Take(limite)
                .Select(r => new AsistenteRegistroItem
                {
                    IdRegistro = r.idAsistenteRegistro,
                    Fecha = r.fecha,
                    Dni = r.dni,
                    Pregunta = r.pregunta,
                    Respuesta = r.respuesta,
                    Herramientas = r.herramientas,
                    Resultado = r.resultado,
                    DuracionMs = r.duracionMs,
                    Tokens = r.tokens,
                    Calificacion = r.calificacion,
                    Comentario = r.comentario,
                })
                .ToList();
        }
    }
}
