using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegVoluntaria
    {
        public readonly VoluntariaRepositorio voluntariaRepositorio;
        private readonly HorarioRepositorio horarioRepositorio;
        public NegVoluntaria()
        {
            voluntariaRepositorio = new VoluntariaRepositorio();
            horarioRepositorio = new HorarioRepositorio();
        }

        private void ValidarVoluntaria(VOLUNTARIA voluntaria)
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

            if (voluntaria.IdEstado.HasValue)
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

        public bool registrarVoluntaria(VOLUNTARIA voluntaria)
        {
            ValidarVoluntaria(voluntaria);
            bool registroVoluntaria = voluntariaRepositorio.registrarVoluntaria(voluntaria);
            return registroVoluntaria;
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
            ValidarVoluntaria(voluntaria);

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
