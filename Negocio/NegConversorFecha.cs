namespace ResimamisBackend.Negocio
{
    public static class NegConversorFecha
    {
        public static DateTime ObtenerFechaArgentina()
        {
            DateTime horaArgentina = DateTime.Now;
            horaArgentina = horaArgentina.AddHours(-3);
            return horaArgentina;
        }
    }
}
