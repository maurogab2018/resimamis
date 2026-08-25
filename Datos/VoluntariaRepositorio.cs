using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class VoluntariaRepositorio : IVoluntariaRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IEstadoRepositorio estadoRepositorio;

        public VoluntariaRepositorio(ApplicationDbContext db, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.estadoRepositorio = estadoRepositorio;
        }

        private int? IdEstadoEliminadoVoluntarias()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Voluntarias")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();
        }

        private int? IdEstadoEliminadoAsistencias()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Asistencias")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();
        }

        public List<VOLUNTARIA> listarVoluntarias()
        {
            var idEl = IdEstadoEliminadoVoluntarias();
            var q = db.VOLUNTARIA
                .AsNoTracking()
                .Include(v => v.RolInfo)
                .Include(v => v.Estado!).ThenInclude(e => e!.ambito)
                .AsQueryable();
            if (idEl != null)
                q = q.Where(v => v.IdEstado != idEl);
            return q.ToList();
        }

        public bool registrarVoluntaria(VOLUNTARIA Voluntaria)
        {
            db.VOLUNTARIA.Add(Voluntaria);
            db.SaveChanges();
            return true;
        }

        public bool cambioEstadoVoluntaria(VOLUNTARIA Voluntaria)
        {
            var voluntaria = db.VOLUNTARIA.Include(v => v.RolInfo).Single(v => v.IdVoluntaria == Voluntaria.IdVoluntaria);
            voluntaria = Voluntaria;
            db.SaveChanges();
            return true;
        }

        public VOLUNTARIA consultarVoluntaria(int Dni)
        {
            var voluntaria = db.VOLUNTARIA
                .Include(v => v.RolInfo)
                .Include(v => v.Estado!).ThenInclude(e => e!.ambito)
                .FirstOrDefault(m => m.IdVoluntaria == Dni);
            //voluntaria.rol = voluntaria.RolInfo.Nombre;
            if (voluntaria == null)
                throw new NotFoundException("Voluntaria no encontrada con ese Id.");
            //var estado = db.ESTADO.SingleOrDefault(e => e.idEstado == voluntaria.IdEstado).nombre;
            //if (estado != null)
            //    voluntaria.estadoVoluntaria = estado;
            return voluntaria;
        }


        public List<VOLUNTARIA> consultarVoluntarias(List<int> idVoluntarias)
        {
            //validacion extra para ver si estan libres
            //var diaHoy = NegConversorFecha.ObtenerFechaArgentina().Date;
            // v => v.Asistencias != null && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso.Value.Date == diaHoy && a.FechaHoraSalida == null) && v.Estado.nombre != "Inactiva" && v.Estado.nombre != "Licencia" && v.Estado.nombre != "Carpeta médica"
            var voluntarias = db.VOLUNTARIA.Include(v => v.RolInfo).Where(m => idVoluntarias.Contains(m.IdVoluntaria)).ToList();

            if (voluntarias == null)
                throw new ApplicationException("Voluntarias no existente con ese Id");
            //var estado = db.ESTADO.SingleOrDefault(e => e.idEstado == voluntaria.IdEstado).nombre;
            //if (estado != null)
            //    voluntaria.estadoVoluntaria = estado;
            return voluntarias;
        }

        public bool eliminarVoluntaria(int dni)
        {
            var voluntaria = db.VOLUNTARIA.FirstOrDefault(m => m.IdVoluntaria == dni);

            if (voluntaria == null)
                throw new NotFoundException("Voluntaria no encontrada con ese Id.");
            voluntaria.IdEstado = estadoRepositorio.ObtenerIdEstadoEliminado("Voluntarias");
            db.SaveChanges();
            return true;
        }

        public bool modificarVoluntaria(VOLUNTARIA voluntaria, VOLUNTARIA voluntariaModificar)
        {
            voluntariaModificar.Apellido = voluntaria.Apellido;
            voluntariaModificar.Nombre = voluntaria.Nombre;
            voluntariaModificar.Celular = voluntaria.Celular;
            voluntariaModificar.FechaFin = voluntaria.FechaFin;
            voluntariaModificar.FechaInicio = voluntaria.FechaInicio;
            voluntariaModificar.Mail = voluntaria.Mail;
            voluntariaModificar.Dni = voluntaria.Dni;
            if (voluntaria.IdEstado.HasValue && voluntaria.IdEstado.Value > 0)
                voluntariaModificar.IdEstado = voluntaria.IdEstado;
            db.SaveChanges();
            return true;
        }
        public List<VOLUNTARIA> obtenerVoluntariasLibres()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idEl = IdEstadoEliminadoVoluntarias();
            var idElimAsist = IdEstadoEliminadoAsistencias();
            var voluntariasLibres = db.VOLUNTARIA.Include(v => v.RolInfo).Include(v => v.Estado).Where(v =>
                (idEl == null || v.IdEstado != idEl)
                && v.Asistencias != null && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.FechaHoraSalida == null && (idElimAsist == null || a.idEstado != idElimAsist)) && v.Estado != null && v.Estado.nombre != "Eliminado" && v.Estado.nombre != "Inactiva" && v.Estado.nombre != "Licencia" && v.Estado.nombre != "Carpeta médica" && v.Estado.nombre != "Creada").Select(v=> new VOLUNTARIA()
            {
                IdVoluntaria= v.IdVoluntaria,
                Dni= v.Dni,
                Nombre= v.Nombre,
                Apellido= v.Apellido,
                Mail= v.Mail,
                Celular= v.Celular,
                FechaInicio= v.FechaInicio,
                FechaFin= v.FechaFin,
                IdEstado=v.IdEstado,
                Asignaciones= v.Asignaciones
            }).ToList();
            if (voluntariasLibres.Count == 0)
                throw new ApplicationException("No hay voluntarias disponibles para el día de hoy");
            return voluntariasLibres;
        }
        public List<VOLUNTARIA> obtenerVoluntariasLibres1()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idEl = IdEstadoEliminadoVoluntarias();
            var idElimAsist = IdEstadoEliminadoAsistencias();
            var voluntariasLibres = db.VOLUNTARIA.Include(v => v.RolInfo).Where(v =>
                (idEl == null || v.IdEstado != idEl)
                && v.Asistencias != null && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.FechaHoraSalida == null && (idElimAsist == null || a.idEstado != idElimAsist))).Select(v => new VOLUNTARIA()
            {
                IdVoluntaria = v.IdVoluntaria,
                Dni = v.Dni,
                Nombre = v.Nombre,
                Apellido = v.Apellido,
                Mail = v.Mail,
                Celular = v.Celular,
                FechaInicio = v.FechaInicio,
                FechaFin = v.FechaFin,
                IdEstado = v.IdEstado,
                //Asignaciones = v.Asignaciones
            }).ToList();
            //if (voluntariasLibres.Count == 0)
            //    throw new ApplicationException("No hay voluntarias disponibles para el día de hoy");
            return voluntariasLibres;
        }

        public VOLUNTARIA? asignarVoluntaria(int idVoluntaria)
        {
            var voluntaria = db.VOLUNTARIA.FirstOrDefault(v => v.IdVoluntaria == idVoluntaria);
            if (voluntaria != null)
            {
                var estadoAsignado = db.ESTADO.FirstOrDefault(e => e.ambito.nombre == "Voluntarias" && e.nombre == "Asignada");
                if (estadoAsignado == null)
                    throw new ApplicationException("Estado asignada inexistente");
                        
                if(voluntaria.IdEstado!=estadoAsignado.idEstado)
                {
                    voluntaria.IdEstado= estadoAsignado.idEstado;
                }
                return voluntaria;
            }
            return null;

        }

        public List<ESTADO> devolverEstadosVoluntarias()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.ambito != null
                            && e.ambito.nombre == "Voluntarias"
                            && e.nombre != "Eliminado")
                .OrderBy(e => e.nombre)
                .ToList();
        }

        public List<VOLUNTARIA> listarVoluntariasSinUsuario()
        {
            var idElVol = IdEstadoEliminadoVoluntarias();
            var idElUsu = db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Usuarios")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();

            var idsVoluntariasConUsuario = db.USUARIO
                .AsNoTracking()
                .Where(u => idElUsu == null || u.idEstado == null || u.idEstado != idElUsu)
                .Select(u => u.IdVoluntaria)
                .Distinct();

            var q = db.VOLUNTARIA
                .Include(v => v.RolInfo)
                .Where(v => !idsVoluntariasConUsuario.Contains(v.IdVoluntaria));

            if (idElVol != null)
                q = q.Where(v => v.IdEstado != idElVol);

            return q
                .OrderBy(v => v.Apellido)
                .ThenBy(v => v.Nombre)
                .ToList();
        }

        public bool existeOtraVoluntariaConDni(int dni, int? exceptIdVoluntaria = null)
        {
            var idEl = IdEstadoEliminadoVoluntarias();
            var q = db.VOLUNTARIA.AsNoTracking().Where(v => v.Dni == dni);
            if (exceptIdVoluntaria.HasValue)
                q = q.Where(v => v.IdVoluntaria != exceptIdVoluntaria.Value);
            if (idEl != null)
                q = q.Where(v => v.IdEstado != idEl);
            return q.Any();
        }

        public bool existeOtraVoluntariaConMail(string mail, int? exceptIdVoluntaria = null)
        {
            if (string.IsNullOrWhiteSpace(mail))
                return false;
            var mailNorm = mail.Trim().ToLowerInvariant();
            var idEl = IdEstadoEliminadoVoluntarias();
            var q = db.VOLUNTARIA.AsNoTracking()
                .Where(v => v.Mail != null && v.Mail.ToLower() == mailNorm);
            if (exceptIdVoluntaria.HasValue)
                q = q.Where(v => v.IdVoluntaria != exceptIdVoluntaria.Value);
            if (idEl != null)
                q = q.Where(v => v.IdEstado != idEl);
            return q.Any();
        }
    }
}

