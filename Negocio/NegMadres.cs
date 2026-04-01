using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegMadres
    {
        public readonly MadreRepositorio repositorioMadre;
        public NegMadres()
        {
            repositorioMadre = new MadreRepositorio();
        }

        public List<MADRE> listarMadres()
        {
            return repositorioMadre.listarMadres();
        }

        public List<EstadisticaLocalidades> devolverEstadisticasLocalidades()
        {
            return repositorioMadre.devolverEstadisticasLocalidades();
        }

        public ResultadoValidacion registrarMadre(MADRE madre)
        {
            //aca van las validaciones
            var resultado = new ResultadoValidacion();

            // Validaciones
            if (string.IsNullOrWhiteSpace(madre.Nombre))
                resultado.Errores.Add("Nombre es obligatorio.");
            else
            {
                if (!Regex.IsMatch(madre.Nombre, @"^[a-zA-Z]+$"))
                    resultado.Errores.Add("Nombre no permite caracteres especiales.");
                if (madre.Nombre.Length > 15)
                    resultado.Errores.Add("Nombre no permite más de 15 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(madre.Apellido))
                resultado.Errores.Add("Apellido es obligatorio.");
            else
            {
                if (!Regex.IsMatch(madre.Apellido, @"^[a-zA-Z]+$"))
                    resultado.Errores.Add("Apellido no permite caracteres especiales.");
                if (madre.Apellido.Length > 20)
                    resultado.Errores.Add("Apellido no permite más de 20 caracteres.");
            }

            if (madre.FechaNacimiento == default)
                resultado.Errores.Add("FechaNacimiento es obligatorio.");

            if (madre.Dni == 0)
                resultado.Errores.Add("Dni es obligatorio.");
            else if (!Regex.IsMatch(madre.Dni.ToString(), @"^\d{7,8}$"))
                resultado.Errores.Add("Dni tiene que tener entre 7 y 8 dígitos.");

            if (madre.Localidad <= 0)
                resultado.Errores.Add("Localidad es obligatorio.");
            else if (!ExisteLocalidad(madre.Localidad))
                resultado.Errores.Add("Localidad no existente con ese ID.");

            if (madre.EstadoCivil <= 0)
                resultado.Errores.Add("EstadoCivil es obligatorio.");
            else if (!ExisteEstadoCivil(madre.EstadoCivil))
                resultado.Errores.Add("EstadoCivil no existente con ese ID.");

            if (madre.CantidadHijos <= 0)
                resultado.Errores.Add("CantidadHijos es obligatorio.");
            else if (madre.CantidadHijos > 10)
                resultado.Errores.Add("CantidadHijos tiene que ser un número entre 1 y 10.");

            if (string.IsNullOrWhiteSpace(madre.MotivoAbrazo))
                resultado.Errores.Add("MotivoAbrazo es obligatorio.");
            else if (madre.MotivoAbrazo.Length > 100)
                resultado.Errores.Add("MotivoAbrazo no permite más de 100 caracteres.");

            var celularStr = madre.Celular.ToString();
            if (madre.Celular == 0)
                resultado.Errores.Add("Celular es obligatorio.");
            else if (!Regex.IsMatch(celularStr, @"^\d{10,13}$"))
                resultado.Errores.Add("Celular tiene que tener entre 10 y 13 dígitos.");

            // Si hay errores, no seguimos
            if (!resultado.Exito)
                return resultado;

            // Si pasa validaciones, registrar
            bool registrado = repositorioMadre.registrarMadre(madre);

            if (!registrado)
                resultado.Errores.Add("Error al registrar la madre.");

            return resultado;
        }

        public MADRE consultarMadre(int id)
        {
            return repositorioMadre.consultarMadre(id);
        }
        private bool ExisteLocalidad(int id)
        {
            // Validar contra base de datos real
            return id >= 1 && id <= 100; // ejemplo
        }

        private bool ExisteEstadoCivil(int id)
        {
            // Validar contra base de datos real
            return id >= 1 && id <= 5; // ejemplo
        }
        public MADRE modificarMadre(MADRE madre,int Id)
        {
            //validaciones
            var madreModificar = repositorioMadre.consultarMadre(Id);
            if (madre.Nombre == null || madre.Nombre.Length > 50)
                throw new ApplicationException("Nombre inválido");
            if (madre.Apellido == null || madre.Apellido.Length > 50)
                throw new ApplicationException("Apellido inválido");
            if (madre.MotivoAbrazo == null || madre.MotivoAbrazo.Length > 500)
                throw new ApplicationException("Motivo abrazo inválido");


            return repositorioMadre.modificarMadre(madre, madreModificar);
        }

        public List<EstadisticaEdadesMadres> devolverEstadisticasEdadesMadres()
        {
            return repositorioMadre.devolverEstadisticasEdadesMadres();
        }
    }
}
