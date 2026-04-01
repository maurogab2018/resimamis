using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class VoluntariaRepositorio
    {
        private readonly ApplicationDbContext db;
        public VoluntariaRepositorio()
        {
            db = new ApplicationDbContext();
        }

        public List<VOLUNTARIA> listarVoluntarias()
        {
            return db.VOLUNTARIA.Include(v => v.RolInfo).Where(v=>v.IdEstado!=7).ToList();
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
                .FirstOrDefault(m => m.IdVoluntaria == Dni);
            //voluntaria.rol = voluntaria.RolInfo.Nombre;
            if (voluntaria == null)
                throw new ApplicationException("Voluntaria no existente con ese Id");
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
                throw new ApplicationException("Voluntaria no existente con ese Id");
            var estado =  db.ESTADO.FirstOrDefault(e => e.ambito.nombre == "Voluntarias" && e.nombre=="Inactiva");

            if (estado == null)
                throw new ApplicationException("No se encontro el estado");
            voluntaria.IdEstado = estado.idEstado;
            db.SaveChangesAsync();
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
            voluntariaModificar.IdEstado=voluntaria.IdEstado;
            db.SaveChangesAsync();
            return true;
        }
        public List<VOLUNTARIA> obtenerVoluntariasLibres()
        {
            var diaHoy = NegConversorFecha.ObtenerFechaArgentina().Date;
            var voluntariasLibres = db.VOLUNTARIA.Include(v => v.RolInfo).Where(v => v.Asistencias != null && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso.Value.Date == diaHoy && a.FechaHoraSalida == null) && v.Estado.nombre!="Inactiva" && v.Estado.nombre != "Licencia" && v.Estado.nombre != "Carpeta médica").Select(v=> new VOLUNTARIA()
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
            var diaHoy = NegConversorFecha.ObtenerFechaArgentina().Date;
            var voluntariasLibres = db.VOLUNTARIA.Include(v => v.RolInfo).Where(v => v.Asistencias != null && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso.Value.Date == diaHoy && a.FechaHoraSalida == null)).Select(v => new VOLUNTARIA()
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
            return db.ESTADO.Where(e => e.ambito.nombre == "Voluntarias" && (e.nombre== "Carpeta médica" || e.nombre == "Inactiva" || e.nombre == "Licencia" || e.nombre == "Activa")).ToList();
        }
    }
}

