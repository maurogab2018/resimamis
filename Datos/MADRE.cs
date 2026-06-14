using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Datos
{
    public class MADRE
    {
        [Key]
        public int IdMadre { get; set; }
        public string Nombre { get; set; }

        public DateTime FechaNacimiento { get; set; }

        public string Apellido { get; set; }

        public int Dni { get; set; }
        public int Localidad { get; set; }

        public virtual LOCALIDAD? LocalidadDetalle { get; set; }

        public int EstadoCivil { get; set; }

        public int CantidadHijos { get; set; }

        /// <summary>Activa / inactiva (legado). La baja lógica usa <see cref="IdEstado"/> = Eliminado (ámbito Madres).</summary>
        public bool Estado { get; set; }

        public string MotivoAbrazo { get; set; }

        public Int64 Celular { get; set; }

        /// <summary>Estado operativo (tabla ESTADO, ámbito Madres), p. ej. Eliminado.</summary>
        public int? IdEstado { get; set; }

        public virtual ESTADO? EstadoDetalle { get; set; }


        public  List<BEBE>? Bebe {get; set;}

        //public int? IdBebe { get; set; }

    }
}
