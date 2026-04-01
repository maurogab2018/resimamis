using System;

namespace ResimamisBackend.Entidades
{
    public class DetalleAsignacionResumido
    {
        public int cantidad { get; set; }
        public int idInsumo { get; set; }

        public string nombreInsumo { get; set; }

        public DateTime fechaEntrega { get;set; }

    }
}
