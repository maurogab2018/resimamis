using Microsoft.EntityFrameworkCore;

namespace ResimamisBackend.Datos
{
    public class EstadoRepositorio
    {
        private readonly ApplicationDbContext db;

        public EstadoRepositorio()
        {
            db = new ApplicationDbContext();
        }

        /// <summary>Resuelve el id de ESTADO con nombre "Eliminado" para el ámbito indicado (Voluntarias, Bebes, Madres).</summary>
        public int ObtenerIdEstadoEliminado(string nombreAmbito)
        {
            var row = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .FirstOrDefault(e => e.nombre == "Eliminado" && e.ambito.nombre == nombreAmbito);
            if (row == null)
                throw new ApplicationException(
                    $"No existe el estado 'Eliminado' para el ámbito '{nombreAmbito}'. Ejecute las migraciones o inserte el registro en ESTADO.");
            return row.idEstado;
        }
    }
}
