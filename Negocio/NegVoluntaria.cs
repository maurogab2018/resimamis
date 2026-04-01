using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class NegVoluntaria
    {
        public readonly VoluntariaRepositorio voluntariaRepositorio;
        public NegVoluntaria()
        {
            voluntariaRepositorio = new VoluntariaRepositorio();
        }

        public List<VOLUNTARIA> listarVoluntarias()
        {
            return voluntariaRepositorio.listarVoluntarias();
        }

        public bool registrarVoluntaria(VOLUNTARIA voluntaria)
        {
            //aca van las validaciones
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

        public bool modificarVoluntaria(VOLUNTARIA voluntaria,int id)
        {
            //validaciones
            var voluntariaModificar = voluntariaRepositorio.consultarVoluntaria(id);
            if (voluntaria.Nombre == null || voluntaria.Nombre.Length > 50)
                throw new ApplicationException("Nombre inválido");
            if (voluntaria.Apellido == null || voluntaria.Apellido.Length > 50)
                throw new ApplicationException("Apellido inválido");


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
