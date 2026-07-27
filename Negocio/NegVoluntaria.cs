using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegVoluntaria : INegVoluntaria
    {
        private readonly IVoluntariaRepositorio voluntariaRepositorio;
        private readonly IHorarioRepositorio horarioRepositorio;
        private readonly IEstadoRepositorio estadoRepositorio;

        public NegVoluntaria(
            IVoluntariaRepositorio voluntariaRepositorio,
            IHorarioRepositorio horarioRepositorio,
            IEstadoRepositorio estadoRepositorio)
        {
            this.voluntariaRepositorio = voluntariaRepositorio;
            this.horarioRepositorio = horarioRepositorio;
            this.estadoRepositorio = estadoRepositorio;
        }

        private void ValidarVoluntaria(VOLUNTARIA voluntaria, bool validarEstado = true)
        {
            if (voluntaria == null)
                throw new ApplicationException("Voluntaria inválida");

            voluntaria.Nombre = ValidacionTextoPersona.Normalizar(voluntaria.Nombre) ?? voluntaria.Nombre;
            voluntaria.Apellido = ValidacionTextoPersona.Normalizar(voluntaria.Apellido) ?? voluntaria.Apellido;

            if (string.IsNullOrWhiteSpace(voluntaria.Nombre) || voluntaria.Nombre.Length > 50)
                throw new ApplicationException("Nombre inválido");
            if (!ValidacionTextoPersona.EsNombreApellidoValido(voluntaria.Nombre))
                throw new ApplicationException("Nombre solo permite letras, espacios y tildes.");

            if (string.IsNullOrWhiteSpace(voluntaria.Apellido) || voluntaria.Apellido.Length > 50)
                throw new ApplicationException("Apellido inválido");
            if (!ValidacionTextoPersona.EsNombreApellidoValido(voluntaria.Apellido))
                throw new ApplicationException("Apellido solo permite letras, espacios y tildes.");

            if (voluntaria.Dni <= 0 || !Regex.IsMatch(voluntaria.Dni.ToString(), @"^\d{7,8}$"))
                throw new ApplicationException("Dni inválido");

            if (string.IsNullOrWhiteSpace(voluntaria.Mail) || voluntaria.Mail.Length > 100)
                throw new ApplicationException("Mail inválido");

            // Validación simple (evita mails obviamente rotos). Si quieren una regla más estricta, se ajusta.
            if (!Regex.IsMatch(voluntaria.Mail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ApplicationException("Mail inválido");

            var celularStr = voluntaria.Celular.ToString();
            if (voluntaria.Celular <= 0 || !Regex.IsMatch(celularStr, @"^\d{10,13}$"))
                throw new ApplicationException("Celular inválido");

            if (voluntaria.FechaInicio == default)
                throw new ApplicationException("FechaInicio inválida");

            if (voluntaria.FechaFin.HasValue && voluntaria.FechaFin.Value < voluntaria.FechaInicio)
                throw new ApplicationException("FechaFin inválida");

            if (validarEstado && voluntaria.IdEstado.HasValue)
            {
                if (voluntaria.IdEstado.Value <= 0)
                    throw new ApplicationException("Estado inválido");

                var estadosValidos = voluntariaRepositorio.devolverEstadosVoluntarias().Select(e => e.idEstado).ToHashSet();
                if (!estadosValidos.Contains(voluntaria.IdEstado.Value))
                    throw new ApplicationException("Estado inválido");
            }

            if (voluntaria.IdRol.HasValue && !RolesVoluntaria.EsRolPermitido(voluntaria.IdRol))
                throw new ApplicationException("Rol inválido. Debe ser Voluntaria o Coordinadora.");
        }

        public List<VOLUNTARIA> listarVoluntarias()
        {
            return voluntariaRepositorio.listarVoluntarias();
        }

        public VoluntariaDetalle registrarVoluntaria(VOLUNTARIA voluntaria, List<HorarioVoluntaria>? horarios = null)
        {
            ValidarVoluntaria(voluntaria, validarEstado: false);

            if (voluntariaRepositorio.existeOtraVoluntariaConDni(voluntaria.Dni))
                throw new ConflictException("Ya existe una voluntaria con ese Dni.");
            if (voluntariaRepositorio.existeOtraVoluntariaConMail(voluntaria.Mail))
                throw new ConflictException("Ya existe una voluntaria con ese mail.");

            voluntaria.IdRol = RolesVoluntaria.IdVoluntaria;
            voluntaria.IdEstado = estadoRepositorio.ObtenerIdEstadoPorNombresYAmbito(
                "Voluntarias", "Creada", "Activa");

            voluntariaRepositorio.registrarVoluntaria(voluntaria);

            var horariosNormalizados = NormalizarHorariosAlta(voluntaria.IdVoluntaria, horarios);
            if (horariosNormalizados.Count > 0)
                horarioRepositorio.registrarHoraraioVoluntaria(horariosNormalizados);

            return consultarVoluntariaDetalle(voluntaria.IdVoluntaria);
        }

        private List<HorarioVoluntaria> NormalizarHorariosAlta(int idVoluntaria, List<HorarioVoluntaria>? horarios)
        {
            if (horarios == null || horarios.Count == 0)
                return new List<HorarioVoluntaria>();

            var normalizados = new List<HorarioVoluntaria>();
            foreach (var h in horarios)
            {
                if (h.IdDia > 0 && !string.IsNullOrWhiteSpace(h.Turno))
                {
                    normalizados.Add(new HorarioVoluntaria
                    {
                        IdVoluntaria = idVoluntaria,
                        IdDia = h.IdDia,
                        Turno = h.Turno.Trim()
                    });
                    continue;
                }

                if (h.IdHorario > 0)
                {
                    var horario = horarioRepositorio.consultarHorario(h.IdHorario);
                    normalizados.Add(new HorarioVoluntaria
                    {
                        IdVoluntaria = idVoluntaria,
                        IdDia = horario.IdDia,
                        Turno = horario.Turno
                    });
                }
            }

            return normalizados;
        }

        public bool eliminarVoluntaria(int id)
        {
            return voluntariaRepositorio.eliminarVoluntaria(id);
        }


        public VOLUNTARIA consultarVoluntaria(int id)
        {
            return voluntariaRepositorio.consultarVoluntaria(id);
        }

        public VoluntariaDetalle consultarVoluntariaDetalle(int id)
        {
            var voluntaria = voluntariaRepositorio.consultarVoluntaria(id);
            var horarios = horarioRepositorio.obtenerHorariosPorVoluntaria(id);
            return MapearVoluntariaDetalle(voluntaria, horarios);
        }

        private static VoluntariaDetalle MapearVoluntariaDetalle(
            VOLUNTARIA voluntaria,
            List<HorarioVoluntariaRespuesta> horarios) =>
            new()
            {
                IdVoluntaria = voluntaria.IdVoluntaria,
                Dni = voluntaria.Dni,
                Nombre = voluntaria.Nombre,
                Apellido = voluntaria.Apellido,
                Mail = voluntaria.Mail,
                Celular = voluntaria.Celular,
                FechaInicio = voluntaria.FechaInicio,
                FechaFin = voluntaria.FechaFin,
                IdEstado = voluntaria.IdEstado,
                IdRol = voluntaria.IdRol,
                Rol = voluntaria.rol,
                Horarios = horarios
            };

        public bool modificarVoluntaria(VOLUNTARIA voluntaria,int id)
        {
            var voluntariaModificar = voluntariaRepositorio.consultarVoluntaria(id);
            ValidarVoluntaria(voluntaria, validarEstado: voluntaria.IdEstado.HasValue && voluntaria.IdEstado.Value > 0);

            if (voluntariaRepositorio.existeOtraVoluntariaConDni(voluntaria.Dni, id))
                throw new ConflictException("Ya existe otra voluntaria con ese Dni.");
            if (voluntariaRepositorio.existeOtraVoluntariaConMail(voluntaria.Mail, id))
                throw new ConflictException("Ya existe otra voluntaria con ese mail.");

            return voluntariaRepositorio.modificarVoluntaria(voluntaria, voluntariaModificar);
        }
        public List<VOLUNTARIA> listarVoluntariasLibres()
        {
            var lista = voluntariaRepositorio.obtenerVoluntariasLibres();
            return lista;
        }
        public List<VOLUNTARIA> listarVoluntariasLibres1()
        {
            var lista = voluntariaRepositorio.obtenerVoluntariasLibres1();
            return lista;
        }
        public List<ESTADO> devolverEstadosVoluntarias()
        {
            return voluntariaRepositorio.devolverEstadosVoluntarias();
        }
    }
}
