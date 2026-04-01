namespace ResimamisBackend.Negocio
{
    public static class NegConversorFecha
    {
        private static TimeZoneInfo ArgentinaTimeZone =>
            LazyArgentinaTz.Value;

        private static readonly Lazy<TimeZoneInfo> LazyArgentinaTz = new(() =>
        {
            foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            throw new InvalidOperationException(
                "No se encontró zona horaria Argentina (America/Argentina/Buenos_Aires / Argentina Standard Time).");
        });

        /// <summary>
        /// Instante actual en UTC. Usar para persistir en PostgreSQL (timestamptz); Npgsql rechaza Kind=Local.
        /// </summary>
        public static DateTime ObtenerFechaArgentina()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Rango UTC [inicio, fin) del día calendario actual en Argentina (para filtrar timestamptz sin usar .Date con Local).
        /// </summary>
        public static (DateTime InicioUtc, DateTime FinUtcExclusivo) RangoDiaHoyArgentinaEnUtc()
        {
            var tz = ArgentinaTimeZone;
            var ahoraEnArgentina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var inicioLocal = new DateTime(
                ahoraEnArgentina.Year,
                ahoraEnArgentina.Month,
                ahoraEnArgentina.Day,
                0, 0, 0,
                DateTimeKind.Unspecified);
            var finLocal = inicioLocal.AddDays(1);
            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
            var finUtc = TimeZoneInfo.ConvertTimeToUtc(finLocal, tz);
            return (inicioUtc, finUtc);
        }
    }
}
