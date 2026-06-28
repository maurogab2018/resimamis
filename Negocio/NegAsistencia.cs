using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegAsistencia : INegAsistencia
    {
        private readonly IAsistenciaRepositorio repositorioAsistencia;
        private readonly IVoluntariaRepositorio repositorioVoluntaria;
        private readonly IHorarioRepositorio repositorioHorario;

        public NegAsistencia(
            IAsistenciaRepositorio repositorioAsistencia,
            IVoluntariaRepositorio repositorioVoluntaria,
            IHorarioRepositorio repositorioHorario)
        {
            this.repositorioAsistencia = repositorioAsistencia;
            this.repositorioVoluntaria = repositorioVoluntaria;
            this.repositorioHorario = repositorioHorario;
        }

        public bool registrarAsistencia(int idVoluntaria )
        {
            var asistencia = new ASISTENCIA();
            var existeVoluntaria = repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
            //var existeHorario = repositorioHorario.consultarHorario(asistencia.IdHorario.Value);
            //asistencia.IdHorario = existeHorario.IdHorario;
            asistencia.IdVoluntaria = existeVoluntaria.IdVoluntaria;
            asistencia.FechaHoraIngreso=NegConversorFecha.ObtenerFechaArgentina();
            asistencia.FechaHoraSalida = null;
            return repositorioAsistencia.registrarAsistencia(asistencia);
        }
        public ASISTENCIA? consultarAsistencia(int idVoluntaria)
        {
            return repositorioAsistencia.consultarAsistencia(idVoluntaria);
        }

        public List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria)
        {
            return repositorioAsistencia.consultarAsistenciasVoluntaria(idVoluntaria);
        }

        public List<ASISTENCIA> consultarAsistenciasFechahoy()
        {
            return repositorioAsistencia.consultarAsistenciasFechahoy();
        }
        public bool registrarAsistenciaSalida(int idVoluntaria)
        {
            var existeVoluntaria = repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
            return repositorioAsistencia.registrarAsistenciaSalida(idVoluntaria);
        }

        public bool eliminarAsistencia(int idAsistencia)
        {
            return repositorioAsistencia.eliminarAsistenciaLogico(idAsistencia);
        }

        public List<ReporteAsistenciaPeriodoItem> ListarTodasAsistencias()
        {
            var rows = repositorioAsistencia.ListarTodasAsistencias();
            return rows.Select(a =>
            {
                double? minutos = null;
                if (a.FechaHoraIngreso.HasValue && a.FechaHoraSalida.HasValue)
                    minutos = (a.FechaHoraSalida.Value - a.FechaHoraIngreso.Value).TotalMinutes;
                return new ReporteAsistenciaPeriodoItem
                {
                    IdAsistencia = a.IdAsistencia ?? 0,
                    IdVoluntaria = a.IdVoluntaria ?? 0,
                    NombreVoluntaria = a.Voluntaria?.Nombre ?? "",
                    ApellidoVoluntaria = a.Voluntaria?.Apellido ?? "",
                    FechaHoraIngreso = a.FechaHoraIngreso,
                    FechaHoraSalida = a.FechaHoraSalida,
                    DuracionMinutos = minutos,
                    EstadoAsistencia = a.Estado?.nombre ?? ""
                };
            }).ToList();
        }

        /// <summary>Reporte de asistencias en un período (fechas inclusive por día calendario en Argentina).</summary>
        public ReporteAsistenciaPeriodoRespuesta ReporteAsistenciaPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
        {
            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var rows = repositorioAsistencia.ListarAsistenciasPorPeriodoUtc(inicioUtc, finUtc);

            var items = rows.Select(a =>
            {
                double? minutos = null;
                if (a.FechaHoraIngreso.HasValue && a.FechaHoraSalida.HasValue)
                    minutos = (a.FechaHoraSalida.Value - a.FechaHoraIngreso.Value).TotalMinutes;

                return new ReporteAsistenciaPeriodoItem
                {
                    IdAsistencia = a.IdAsistencia ?? 0,
                    IdVoluntaria = a.IdVoluntaria ?? 0,
                    NombreVoluntaria = a.Voluntaria?.Nombre ?? "",
                    ApellidoVoluntaria = a.Voluntaria?.Apellido ?? "",
                    FechaHoraIngreso = a.FechaHoraIngreso,
                    FechaHoraSalida = a.FechaHoraSalida,
                    DuracionMinutos = minutos,
                    EstadoAsistencia = a.Estado?.nombre ?? ""
                };
            }).ToList();

            return new ReporteAsistenciaPeriodoRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                TotalRegistros = items.Count,
                Registros = items
            };
        }
    }
}

