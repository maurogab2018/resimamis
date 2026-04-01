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
        public bool registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria)
        {
            if (horarioVoluntaria != null)
            {
                foreach(HorarioVoluntaria h in horarioVoluntaria) 
                {
                    var dia = db.DIA.FirstOrDefault(d => d.IdDia == h.IdDia);
                    if (dia == null)
                        throw new ApplicationException("Día no existente con es id");
                    var voluntaria = db.VOLUNTARIA.FirstOrDefault(v => v.IdVoluntaria == h.IdVoluntaria);
                    if (voluntaria == null)
                        throw new ApplicationException("Voluntaria ya existente con ese id");
                    var horario = db.HORARIO.FirstOrDefault(ho => ho.Turno == h.Turno && ho.IdDia == dia.IdDia);
                    if (horario == null)
                        throw new ApplicationException("Horario no existente para ese día o turno");
                    var a = db.VOLUNTARIAHORARIO.FirstOrDefault(ho => ho.IdHorario == horario.IdHorario && ho.IdHorarioVoluntaria == h.IdVoluntaria);
                    if (db.VOLUNTARIAHORARIO.Any(ho => ho.IdHorario == horario.IdHorario && ho.IdVoluntaria==h.IdVoluntaria))
                        throw new ApplicationException("Horario ya asignado para ese día");
                    var horarioVoluntariaNuevo = new VOLUNTARIAHORARIO() { IdHorario=horario.IdHorario,IdVoluntaria=voluntaria.IdVoluntaria};
                    db.VOLUNTARIAHORARIO.Add(horarioVoluntariaNuevo);
                    db.SaveChanges();
                }
                return true;
            }
            return false;
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
    }
}
