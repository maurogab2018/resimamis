using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegHorariosVoluntaria : INegHorariosVoluntaria
    {
        private readonly IHorarioRepositorio horarioRepositorio;

        public NegHorariosVoluntaria(IHorarioRepositorio horarioRepositorio)
        {
            this.horarioRepositorio = horarioRepositorio;
        }

        private static void ValidarHorarios(IEnumerable<HorarioVoluntaria> horarios, int? forzarIdVoluntaria = null)
        {
            var lista = horarios?.ToList() ?? new List<HorarioVoluntaria>();
            for (var i = 0; i < lista.Count; i++)
            {
                var h = lista[i];
                var prefijo = $"Horario (índice {i + 1}): ";
                if (forzarIdVoluntaria.HasValue)
                    h.IdVoluntaria = forzarIdVoluntaria.Value;

                if (h.IdVoluntaria <= 0)
                    throw new ApplicationException(prefijo + "IdVoluntaria es obligatorio.");
                if (h.IdDia <= 0)
                    throw new ApplicationException(prefijo + "IdDia es obligatorio.");
                if (string.IsNullOrWhiteSpace(h.Turno))
                    throw new ApplicationException(prefijo + "Turno es obligatorio.");
                h.Turno = h.Turno.Trim();
            }

            var duplicados = lista
                .GroupBy(h => new { h.IdVoluntaria, h.IdDia, Turno = h.Turno.Trim().ToUpperInvariant() })
                .Where(g => g.Count() > 1)
                .Select(g => $"día {g.Key.IdDia} turno {g.Key.Turno}")
                .ToList();
            if (duplicados.Count > 0)
                throw new ApplicationException("Hay horarios duplicados en la solicitud: " + string.Join(", ", duplicados) + ".");
        }

        public List<HorarioVoluntariaRespuesta> registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria)
        {
            if (horarioVoluntaria == null || horarioVoluntaria.Count == 0)
                throw new ApplicationException("Debe indicar al menos un horario.");

            ValidarHorarios(horarioVoluntaria);
            horarioRepositorio.registrarHoraraioVoluntaria(horarioVoluntaria);
            return horarioVoluntaria
                .Select(h => h.IdVoluntaria)
                .Distinct()
                .SelectMany(id => horarioRepositorio.obtenerHorariosPorVoluntaria(id))
                .OrderBy(h => h.IdVoluntaria)
                .ThenBy(h => h.IdDia)
                .ThenBy(h => h.Turno)
                .ToList();
        }

        public List<HorarioVoluntariaRespuesta> reemplazarHorarios(int idVoluntaria, List<HorarioVoluntaria> horarios)
        {
            if (idVoluntaria <= 0)
                throw new ApplicationException("Id de voluntaria inválido.");

            var lista = horarios ?? new List<HorarioVoluntaria>();
            ValidarHorarios(lista, forzarIdVoluntaria: idVoluntaria);
            horarioRepositorio.reemplazarHorarios(idVoluntaria, lista);

            return horarioRepositorio.obtenerHorariosPorVoluntaria(idVoluntaria);
        }

        public List<DIA> obtenerDias()
        {
            return horarioRepositorio.obtenerDias();
        }

        public List<HorarioVoluntariaRespuesta> obtenerHorariosPorVoluntaria(int idVoluntaria)
        {
            if (idVoluntaria <= 0)
                throw new ApplicationException("Id de voluntaria inválido.");
            return horarioRepositorio.obtenerHorariosPorVoluntaria(idVoluntaria);
        }

        public bool eliminarHorarioVoluntaria(int idHorarioVoluntaria)
        {
            if (idHorarioVoluntaria <= 0)
                throw new ApplicationException("Id de horario de voluntaria inválido.");
            return horarioRepositorio.eliminarHorarioVoluntariaLogico(idHorarioVoluntaria);
        }
    }
}
