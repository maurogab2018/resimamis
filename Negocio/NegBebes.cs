using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegBebes
    {
        private readonly BebeRepositorio repositorioBebe;
        public NegBebes()
        {
            repositorioBebe = new BebeRepositorio();
        }

        /// <summary>Reglas alineadas con modificar/registrar: nombre, apellido, sexo, dni opcional, fecha nacimiento.</summary>
        public static void ValidarCamposBebe(BEBE bebe, ResultadoValidacion resultado, string prefijo = "")
        {
            if (bebe == null)
            {
                resultado.Errores.Add(prefijo + "Bebé inválido.");
                return;
            }

            if (string.IsNullOrWhiteSpace(bebe.nombre))
                resultado.Errores.Add(prefijo + "Nombre es obligatorio.");
            else if (bebe.nombre.Length > 50)
                resultado.Errores.Add(prefijo + "Nombre no permite más de 50 caracteres.");

            if (string.IsNullOrWhiteSpace(bebe.apellido))
                resultado.Errores.Add(prefijo + "Apellido es obligatorio.");
            else if (bebe.apellido.Length > 50)
                resultado.Errores.Add(prefijo + "Apellido no permite más de 50 caracteres.");

            if (string.IsNullOrWhiteSpace(bebe.Sexo))
                resultado.Errores.Add(prefijo + "Sexo es obligatorio.");
            else if (!Regex.IsMatch(bebe.Sexo.Trim(), @"^(M|F|m|f)$"))
                resultado.Errores.Add(prefijo + "Sexo debe ser M o F.");

            if (bebe.Dni.HasValue && bebe.Dni.Value > 0)
            {
                if (!Regex.IsMatch(bebe.Dni.Value.ToString(), @"^\d{7,8}$"))
                    resultado.Errores.Add(prefijo + "Dni tiene que tener entre 7 y 8 dígitos.");
            }

            if (bebe.FechaNacimiento == null || bebe.FechaNacimiento == default)
                resultado.Errores.Add(prefijo + "FechaNacimiento es obligatorio.");
        }

        public List<BEBE> listarBebes()
        {
            return repositorioBebe.listarBebes();
        }

        public List<SALA> listarSalas()
        {
            return repositorioBebe.listarSalas();
        }

        public bool registrarBebe(BEBE bebe)
        {
            var resultado = new ResultadoValidacion();
            ValidarCamposBebe(bebe, resultado);
            if (!resultado.Exito)
                throw new ApplicationException(string.Join(" ", resultado.Errores));

            bool registroBebe = repositorioBebe.registrarBebe(bebe);
            return registroBebe;
        }

        public BEBE consultarBebe(int id)
        {
            return repositorioBebe.consultarBebe(id);
        }

        public bool modificarBebe(BEBE bebe)
        {
            var resultado = new ResultadoValidacion();
            ValidarCamposBebe(bebe, resultado);
            if (!resultado.Exito)
                throw new ApplicationException(string.Join(" ", resultado.Errores));

            var bebeModificar = repositorioBebe.consultarBebe(bebe.ID);
            return repositorioBebe.modificarBebe(bebe, bebeModificar);
        }

        public bool eliminarBebe(int idBebe)
        {
            return repositorioBebe.eliminarBebeLogico(idBebe);
        }

        public List<BEBE> listarBebesAbrazar()
        {
            return repositorioBebe.obtenerBebesAbrazar();
        }
    }
}
