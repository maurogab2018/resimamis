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
        private readonly IEstadoRepositorio estadoRepositorio;

        public NegAsistencia(
            IAsistenciaRepositorio repositorioAsistencia,
            IVoluntariaRepositorio repositorioVoluntaria,
            IHorarioRepositorio repositorioHorario,
            IEstadoRepositorio estadoRepositorio)
        {
            this.estadoRepositorio = estadoRepositorio;
            this.repositorioAsistencia = repositorioAsistencia;
            this.repositorioVoluntaria = repositorioVoluntaria;
            this.repositorioHorario = repositorioHorario;
        }

        public bool registrarAsistencia(int idVoluntaria )
        {
            if (idVoluntaria <= 0)
                throw new ApplicationException("Id de voluntaria inválido.");

            var asistencia = new ASISTENCIA();
            var existeVoluntaria = repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
            if (existeVoluntaria.Estado != null
                && (string.Equals(existeVoluntaria.Estado.nombre, "Eliminado", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(existeVoluntaria.Estado.nombre, "Inactiva", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApplicationException("La voluntaria no está disponible para registrar asistencia.");
            }

            asistencia.IdVoluntaria = existeVoluntaria.IdVoluntaria;
            asistencia.FechaHoraIngreso=NegConversorFecha.ObtenerFechaArgentina();
            asistencia.FechaHoraSalida = null;
            var ok = repositorioAsistencia.registrarAsistencia(asistencia);
            if (!ok)
                throw new ApplicationException("Ya hay una jornada abierta: registrá la salida antes de marcar una nueva entrada.");

            // El alta deja a la voluntaria en "Creada" y la generación de asignaciones excluye ese
            // estado, así que una voluntaria nueva figuraba libre pero no se le podía asignar nada.
            // Al marcar su primera entrada ya está operativa.
            if (string.Equals(existeVoluntaria.Estado?.nombre, "Creada", StringComparison.OrdinalIgnoreCase))
            {
                existeVoluntaria.IdEstado = estadoRepositorio.ObtenerIdVoluntariaDisponible();
                repositorioVoluntaria.cambioEstadoVoluntaria(existeVoluntaria);
            }
            return true;
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
            if (idVoluntaria <= 0)
                throw new ApplicationException("Id de voluntaria inválido.");

            repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
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

        public DiasTurnosVoluntariasRespuesta ListarDiasTurnosVoluntarias(DateTime fechaInicio, DateTime fechaFin, int? idVoluntaria)
        {
            if (idVoluntaria is <= 0)
                idVoluntaria = null;
            if (idVoluntaria.HasValue)
                repositorioVoluntaria.consultarVoluntaria(idVoluntaria.Value);

            var (inicioUtc, finUtc) = NegConversorFecha.RangoFechasArgentinaEnUtc(fechaInicio, fechaFin);
            var items = repositorioAsistencia.ListarDiasTurnosVoluntarias(inicioUtc, finUtc, idVoluntaria);

            return new DiasTurnosVoluntariasRespuesta
            {
                FechaInicio = DateOnly.FromDateTime(fechaInicio.Date),
                FechaFin = DateOnly.FromDateTime(fechaFin.Date),
                IdVoluntaria = idVoluntaria,
                TotalRegistros = items.Count,
                Registros = items
            };
        }
    }
}

