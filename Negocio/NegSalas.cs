using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegSalas : INegSalas
    {
        private readonly ISalaRepositorio salaRepositorio;

        public NegSalas(ISalaRepositorio salaRepositorio)
        {
            this.salaRepositorio = salaRepositorio;
        }

        private static void ValidarSala(SALA sala)
        {
            if (sala == null)
                throw new ApplicationException("Sala inválida.");
            if (string.IsNullOrWhiteSpace(sala.Nombre))
                throw new ApplicationException("El nombre es obligatorio.");
            if (sala.Nombre.Trim().Length > 100)
                throw new ApplicationException("El nombre no permite más de 100 caracteres.");
        }

        public List<SALA> listarSalas()
        {
            return salaRepositorio.listarSalas();
        }

        public List<SALA> listarSalasActivas()
        {
            return salaRepositorio.listarSalasActivas();
        }

        public SALA consultarSala(int idSala)
        {
            var sala = salaRepositorio.obtenerPorId(idSala);
            if (sala == null)
                throw new NotFoundException("Sala inexistente con ese id.");
            return sala;
        }

        public bool registrarSala(SALA sala)
        {
            ValidarSala(sala);
            sala.Nombre = sala.Nombre.Trim();
            sala.Activa = true;
            if (salaRepositorio.existeOtraSalaConNombre(sala.Nombre))
                throw new ConflictException("Ya existe una sala con ese nombre.");
            return salaRepositorio.registrarSala(sala);
        }

        public bool modificarSala(int idSala, SALA sala)
        {
            ValidarSala(sala);
            sala.Nombre = sala.Nombre.Trim();
            if (salaRepositorio.existeOtraSalaConNombre(sala.Nombre, idSala))
                throw new ConflictException("Ya existe otra sala con ese nombre.");
            var existente = salaRepositorio.obtenerParaModificar(idSala);
            return salaRepositorio.modificarSala(sala, existente);
        }

        public bool eliminarSala(int idSala)
        {
            if (salaRepositorio.tieneBebesActivosAsignados(idSala))
                throw new ApplicationException("No se puede dar de baja: la sala tiene bebés asignados.");
            return salaRepositorio.eliminarSalaLogica(idSala);
        }

        public void ValidarSalaActivaParaBebe(int? idSala)
        {
            if (!idSala.HasValue)
                return;
            var sala = salaRepositorio.obtenerPorId(idSala.Value);
            if (sala == null)
                throw new NotFoundException("Sala no encontrada.");
            if (!sala.Activa)
                throw new ApplicationException("La sala no está activa.");
        }
    }
}
