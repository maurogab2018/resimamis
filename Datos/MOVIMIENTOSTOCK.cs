using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class MOVIMIENTOSTOCK
    {
        [Key]
        public int idMovimiento { get; set; }
        public int idInsumo { get; set; }
        public int? idBebe { get; set; }
        public int? idVoluntaria { get; set; }
        public DateTime? fechaMovimiento { get; set; }

        public string observacion { get; set; }

        public int? cantidad { get; set; }
        public string esEntrada { get; set; }

        public int? idProveedor { get; set; }
    }
}
