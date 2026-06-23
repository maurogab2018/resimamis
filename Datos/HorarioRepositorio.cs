using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class HorarioRepositorio
    {
        private readonly ApplicationDbContext db;
        public HorarioRepositorio()
        {
            db = new ApplicationDbContext();
        }

        private static bool EsHorarioVoluntariaActivo(VOLUNTARIAHORARIO vh) => vh.Activa;

        public bool registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria)
        {
            if (horarioVoluntaria == null || horarioVoluntaria.Count == 0)
                return false;

            foreach (HorarioVoluntaria h in horarioVoluntaria)
            {
                var dia = db.DIA.FirstOrDefault(d => d.IdDia == h.IdDia);
                if (dia == null)
                    throw new ApplicationException("Día no existente con es id");
                var voluntaria = db.VOLUNTARIA.FirstOrDefault(v => v.IdVoluntaria == h.IdVoluntaria);
                if (voluntaria == null)
                    throw new ApplicationException("Voluntaria inexistente con ese id");

                var horario = db.HORARIO.FirstOrDefault(ho => ho.Turno == h.Turno && ho.IdDia == dia.IdDia);
                if (horario == null)
                    throw new ApplicationException("Horario no existente para ese día o turno");

                var existente = db.VOLUNTARIAHORARIO.FirstOrDefault(vh =>
                    vh.IdHorario == horario.IdHorario && vh.IdVoluntaria == h.IdVoluntaria);

                if (existente != null)
                {
                    if (EsHorarioVoluntariaActivo(existente))
                        throw new ApplicationException("Horario ya asignado para ese día");

                    existente.Activa = true;
                    db.SaveChanges();
                    continue;
                }

                var horarioVoluntariaNuevo = new VOLUNTARIAHORARIO
                {
                    IdHorario = horario.IdHorario,
                    IdVoluntaria = voluntaria.IdVoluntaria,
                    Activa = true
                };
                db.VOLUNTARIAHORARIO.Add(horarioVoluntariaNuevo);
                db.SaveChanges();
            }

            return true;
        }

        public bool eliminarHorarioVoluntariaLogico(int idHorarioVoluntaria)
        {
            var row = db.VOLUNTARIAHORARIO.FirstOrDefault(vh => vh.IdHorarioVoluntaria == idHorarioVoluntaria);
            if (row == null)
                throw new NotFoundException("Horario de voluntaria no encontrado con ese Id.");

            if (!row.Activa)
                return true;

            row.Activa = false;
            db.SaveChanges();
            return true;
        }

        public HORARIO consultarHorario(int id)
        {
            var horario = db.HORARIO.FirstOrDefault(h => h.IdHorario == id);
            if (horario == null)
                throw new ApplicationException("No existe horario con ese id");
            return horario;
        }

        public List<DIA> obtenerDias()
        {
            return db.DIA.ToList();
        }

        public List<HorarioVoluntariaRespuesta> obtenerHorariosPorVoluntaria(int idVoluntaria)
        {
            var existe = db.VOLUNTARIA.AsNoTracking().Any(v => v.IdVoluntaria == idVoluntaria);
            if (!existe)
                throw new NotFoundException("Voluntaria no encontrada con ese Id.");

            return db.VOLUNTARIAHORARIO
                .AsNoTracking()
                .Where(vh => vh.IdVoluntaria == idVoluntaria && vh.Activa)
                .Join(
                    db.HORARIO.AsNoTracking(),
                    vh => vh.IdHorario,
                    h => h.IdHorario,
                    (vh, h) => new { vh, h })
                .Join(
                    db.DIA.AsNoTracking(),
                    x => x.h.IdDia,
                    d => d.IdDia,
                    (x, d) => new HorarioVoluntariaRespuesta
                    {
                        IdHorarioVoluntaria = x.vh.IdHorarioVoluntaria,
                        IdHorario = x.h.IdHorario,
                        IdVoluntaria = x.vh.IdVoluntaria,
                        IdDia = d.IdDia,
                        Dia = d.Descripcion,
                        Turno = x.h.Turno,
                        HoraIngreso = x.h.HoraIngreso,
                        HoraSalida = x.h.HoraSalida,
                        Activa = x.vh.Activa
                    })
                .OrderBy(h => h.IdDia)
                .ThenBy(h => h.Turno)
                .ToList();
        }
    }
}
