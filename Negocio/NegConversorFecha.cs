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

        /// <summary>
        /// Rango UTC [inicio, fin) para días calendario inclusivos en Argentina (fechaInicio y fechaFin son fechas locales AR).
        /// </summary>
        /// <param name="maxDiasPermitidos">Límite de amplitud del reporte (inclusive).</param>
        public static (DateTime InicioUtc, DateTime FinUtcExclusivo) RangoFechasArgentinaEnUtc(
            DateTime fechaInicio,
            DateTime fechaFin,
            int maxDiasPermitidos = 731)
        {
            var d0 = fechaInicio.Date;
            var d1 = fechaFin.Date;
            if (d1 < d0)
                throw new ApplicationException("La fecha fin no puede ser anterior a la fecha inicio.");

            var dias = (d1 - d0).Days + 1;
            if (dias > maxDiasPermitidos)
                throw new ApplicationException($"El período no puede superar {maxDiasPermitidos} días.");

            var tz = ArgentinaTimeZone;
            var inicioLocal = new DateTime(d0.Year, d0.Month, d0.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var finExclusivoLocal = new DateTime(d1.Year, d1.Month, d1.Day, 0, 0, 0, DateTimeKind.Unspecified).AddDays(1);
            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
            var finUtc = TimeZoneInfo.ConvertTimeToUtc(finExclusivoLocal, tz);
            return (inicioUtc, finUtc);
        }
    }
}
