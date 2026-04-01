using System;
using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class DETALLEASIGNACION
    {
        [Key]
        public int idDetalleAsignacion { get; set; }    
        public int cantidad { get; set; }
        public DateTime? fechaRetiro { get; set; }
        public DateTime? fechaEntrega { get; set; }
    
        public string? nombreInsumo { get; set; }
        public int idAsignacion { get; set; }
        public int idInsumo { get; set; }

        public virtual ASIGNACION asignacion { get; set; }

        public virtual INSUMO insumo { get; set; }

    }
}
