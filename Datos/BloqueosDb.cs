using Microsoft.EntityFrameworkCore;

namespace ResimamisBackend.Datos
{
    /// <summary>
    /// Pone en fila operaciones que leen y después escriben (doble clic, dos personas a la vez).
    /// Sin esto, 8 salidas de stock simultáneas entregaban 64 unidades de un stock de 10, una
    /// voluntaria quedaba con varias jornadas abiertas y se duplicaban madres por DNI.
    /// Usa pg_advisory_xact_lock: se libera solo al terminar la transacción.
    /// </summary>
    public static class BloqueosDb
    {
        // 7311001 lo usa NegAsignacion al generar asignaciones.
        public const long Stock = 7311002;
        public const long Asistencia = 7311003;
        public const long Abrazos = 7311004;
        public const long Altas = 7311005;
        public const long Asistente = 7311006;

        /// <summary>Toma el lock dentro de la transacción actual (no hace nada fuera de Postgres).</summary>
        public static void Tomar(ApplicationDbContext db, long clave)
        {
            if (db.Database.IsRelational())
                db.Database.ExecuteSqlRaw("SELECT pg_advisory_xact_lock({0})", clave);
        }

        /// <summary>
        /// Ejecuta <paramref name="accion"/> con el lock tomado y todo en una transacción. Si ya hay
        /// una transacción abierta la reutiliza (el lock es reentrante en la misma conexión).
        /// </summary>
        public static T Ejecutar<T>(ApplicationDbContext db, long clave, Func<T> accion)
        {
            if (db.Database.CurrentTransaction != null)
            {
                Tomar(db, clave);
                return accion();
            }

            using var tx = db.Database.BeginTransaction();
            Tomar(db, clave);
            var resultado = accion();
            tx.Commit();
            return resultado;
        }
    }
}
