namespace ResimamisBackend.Negocio
{
    /// <summary>
    /// JSON omite campos y System.Text.Json los deja en default (0, DateTime vacía, false).
    /// Esos defaults no deben pisar el valor persistido en un PUT parcial.
    /// </summary>
    public static class ParchePropiedades
    {
        public static bool EsAusente(object? nuevoValor)
        {
            if (nuevoValor is null)
                return true;

            var t = Nullable.GetUnderlyingType(nuevoValor.GetType()) ?? nuevoValor.GetType();

            if (t == typeof(string))
                return string.IsNullOrWhiteSpace((string)nuevoValor);

            if (t == typeof(DateTime))
                return (DateTime)nuevoValor == default;

            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            {
                try
                {
                    return Convert.ToDecimal(nuevoValor) == 0;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }

            return false;
        }

        public static void CopiarPresentes(object origenParcial, object destino, Func<string, bool>? ignorar = null)
        {
            foreach (var prop in origenParcial.GetType().GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;
                if (ignorar?.Invoke(prop.Name) == true)
                    continue;

                var destProp = destino.GetType().GetProperty(prop.Name);
                if (destProp == null || !destProp.CanWrite)
                    continue;

                var nuevo = prop.GetValue(origenParcial);
                if (EsAusente(nuevo))
                    continue;

                var actual = destProp.GetValue(destino);
                if (nuevo != null && !nuevo.Equals(actual))
                    destProp.SetValue(destino, nuevo);
            }
        }
    }
}
