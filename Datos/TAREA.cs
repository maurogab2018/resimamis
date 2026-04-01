using System.ComponentModel.DataAnnotations;

namespace ResimamisBackend.Datos
{
    public class TAREA
    {
        [Key]

        public int idTarea { get; set; }

        public string nombre { get; set; }

        public bool Estado { get; set; }

        public List<ASIGNACION>? Asignaciones { get; set; }

    }
}
