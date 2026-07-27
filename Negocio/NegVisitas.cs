using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegVisitas : INegVisitas
    {
        private readonly IVisitaRepositorio visitaRepositorio;
        private readonly IBebeRepositorio bebeRepositorio;

        public NegVisitas(IVisitaRepositorio visitaRepositorio, IBebeRepositorio bebeRepositorio)
        {
            this.visitaRepositorio = visitaRepositorio;
            this.bebeRepositorio = bebeRepositorio;
        }

        private static void CombinarVisitaParaModificar(VISITA datos, VISITA existente)
        {
            if (datos.idBebe <= 0)
                datos.idBebe = existente.idBebe;

            if (string.IsNullOrWhiteSpace(datos.nombreVisitante))
                datos.nombreVisitante = existente.nombreVisitante;

            if (string.IsNullOrWhiteSpace(datos.familiar))
                datos.familiar = existente.familiar;

            if (datos.fechaHoraVisita == default)
                datos.fechaHoraVisita = existente.fechaHoraVisita;

            if (datos.observacion == null)
                datos.observacion = existente.observacion;

            if (datos.documentoVisitante == null)
                datos.documentoVisitante = existente.documentoVisitante;

            if (datos.telefonoVisitante == null)
                datos.telefonoVisitante = existente.telefonoVisitante;
        }

        private static void ValidarVisita(VISITA visita)
        {
            if (visita == null)
                throw new ApplicationException("Visita inválida.");

            if (visita.idBebe <= 0)
                throw new ApplicationException("Debe indicar el bebé visitado.");

            if (string.IsNullOrWhiteSpace(visita.nombreVisitante))
                throw new ApplicationException("El nombre del visitante es obligatorio.");
            visita.nombreVisitante = visita.nombreVisitante.Trim();
            if (!ValidacionTextoPersona.EsNombreApellidoValido(visita.nombreVisitante))
                throw new ApplicationException("El nombre del visitante solo permite letras, espacios y tildes.");
            if (visita.nombreVisitante.Length > 100)
                throw new ApplicationException("El nombre del visitante no permite más de 100 caracteres.");

            if (string.IsNullOrWhiteSpace(visita.familiar))
                throw new ApplicationException("El vínculo familiar es obligatorio.");
            visita.familiar = visita.familiar.Trim();
            if (visita.familiar.Length > 50)
                throw new ApplicationException("El vínculo familiar no permite más de 50 caracteres.");

            if (visita.fechaHoraVisita == default)
                throw new ApplicationException("La fecha y hora de la visita es obligatoria.");

            if (!string.IsNullOrWhiteSpace(visita.observacion) && visita.observacion.Length > 500)
                throw new ApplicationException("La observación no permite más de 500 caracteres.");

            if (visita.documentoVisitante.HasValue && visita.documentoVisitante.Value > 0)
            {
                if (!Regex.IsMatch(visita.documentoVisitante.Value.ToString(), @"^\d{7,8}$"))
                    throw new ApplicationException("El documento del visitante debe tener entre 7 y 8 dígitos.");
            }

            if (visita.telefonoVisitante.HasValue && visita.telefonoVisitante.Value > 0)
            {
                var tel = visita.telefonoVisitante.Value.ToString();
                if (!Regex.IsMatch(tel, @"^\d{10,13}$"))
                    throw new ApplicationException("El teléfono del visitante debe tener entre 10 y 13 dígitos.");
            }
        }

        private void AsegurarBebeValido(int idBebe)
        {
            var bebe = bebeRepositorio.consultarBebe(idBebe);
            if (bebe.FechaSalida.HasValue)
                throw new ApplicationException("No se puede registrar/modificar visita: el bebé ya tiene fecha de salida.");
            if (bebe.Estado != null
                && string.Equals(bebe.Estado.nombre, "Eliminado", StringComparison.OrdinalIgnoreCase))
                throw new ApplicationException("No se puede registrar/modificar visita: el bebé está dado de baja.");
        }

        public List<VisitaListado> listarVisitas()
        {
            return visitaRepositorio.listarVisitas();
        }

        public List<VisitaListado> listarVisitasPorBebe(int idBebe)
        {
            return visitaRepositorio.listarVisitasPorBebe(idBebe);
        }

        public VisitaListado consultarVisita(int idVisita)
        {
            var visita = visitaRepositorio.obtenerPorId(idVisita);
            if (visita == null)
                throw new NotFoundException("Visita inexistente o dada de baja.");

            return new VisitaListado
            {
                idVisita = visita.idVisita,
                idBebe = visita.idBebe,
                nombreBebe = visita.Bebe?.nombre,
                apellidoBebe = visita.Bebe?.apellido,
                nombreVisitante = visita.nombreVisitante,
                familiar = visita.familiar,
                fechaHoraVisita = visita.fechaHoraVisita,
                observacion = visita.observacion,
                documentoVisitante = visita.documentoVisitante,
                telefonoVisitante = visita.telefonoVisitante,
                activa = visita.Activa,
                fechaRegistro = visita.fechaRegistro
            };
        }

        public VISITA registrarVisita(VISITA visita)
        {
            ValidarVisita(visita);
            AsegurarBebeValido(visita.idBebe);
            visita.Activa = true;
            if (visita.fechaRegistro == default)
                visita.fechaRegistro = DateTime.UtcNow;
            visita.observacion = string.IsNullOrWhiteSpace(visita.observacion)
                ? null
                : visita.observacion.Trim();
            visitaRepositorio.registrarVisita(visita);
            return visita;
        }

        public bool modificarVisita(int idVisita, VISITA visita)
        {
            if (visita == null)
                throw new ApplicationException("Visita inválida.");

            var existente = visitaRepositorio.obtenerParaModificar(idVisita);
            CombinarVisitaParaModificar(visita, existente);

            ValidarVisita(visita);
            AsegurarBebeValido(visita.idBebe);
            visita.observacion = string.IsNullOrWhiteSpace(visita.observacion)
                ? null
                : visita.observacion.Trim();
            return visitaRepositorio.modificarVisita(visita, existente);
        }

        public bool eliminarVisita(int idVisita)
        {
            return visitaRepositorio.eliminarVisitaLogica(idVisita);
        }
    }
}
