using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegBebes : INegBebes
    {
        private readonly IBebeRepositorio repositorioBebe;
        private readonly INegSalas negSalas;
        private readonly IGenericosRepositorio genericosRepositorio;

        public NegBebes(IBebeRepositorio repositorioBebe, INegSalas negSalas, IGenericosRepositorio genericosRepositorio)
        {
            this.repositorioBebe = repositorioBebe;
            this.negSalas = negSalas;
            this.genericosRepositorio = genericosRepositorio;
        }

        /// <summary>Reglas alineadas con modificar/registrar: nombre, apellido, sexo, dni opcional, fecha nacimiento.</summary>
        public void ValidarCamposBebe(BEBE bebe, ResultadoValidacion resultado, string prefijo = "")
        {
            if (bebe == null)
            {
                resultado.Errores.Add(prefijo + "Bebé inválido.");
                return;
            }

            bebe.nombre = ValidacionTextoPersona.Normalizar(bebe.nombre) ?? bebe.nombre;
            bebe.apellido = ValidacionTextoPersona.Normalizar(bebe.apellido) ?? bebe.apellido;

            if (string.IsNullOrWhiteSpace(bebe.nombre))
                resultado.Errores.Add(prefijo + "Nombre es obligatorio.");
            else
            {
                if (!ValidacionTextoPersona.EsNombreApellidoValido(bebe.nombre))
                    resultado.Errores.Add(prefijo + "Nombre solo permite letras, espacios y tildes.");
                else if (bebe.nombre.Trim().Length > 50)
                    resultado.Errores.Add(prefijo + "Nombre no permite más de 50 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(bebe.apellido))
                resultado.Errores.Add(prefijo + "Apellido es obligatorio.");
            else
            {
                if (!ValidacionTextoPersona.EsNombreApellidoValido(bebe.apellido))
                    resultado.Errores.Add(prefijo + "Apellido solo permite letras, espacios y tildes.");
                else if (bebe.apellido.Trim().Length > 50)
                    resultado.Errores.Add(prefijo + "Apellido no permite más de 50 caracteres.");
            }

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

            if (bebe.IdLocalidad.HasValue)
            {
                if (bebe.IdLocalidad.Value <= 0)
                    bebe.IdLocalidad = null;
                else if (!genericosRepositorio.existeLocalidad(bebe.IdLocalidad.Value))
                    resultado.Errores.Add(prefijo + "Localidad no existente con ese ID.");
            }

            if (bebe.IdSala.HasValue)
            {
                if (bebe.IdSala.Value <= 0)
                    bebe.IdSala = null;
                else
                {
                    try
                    {
                        negSalas.ValidarSalaActivaParaBebe(bebe.IdSala);
                    }
                    catch (NotFoundException)
                    {
                        resultado.Errores.Add(prefijo + "Sala no existente con ese ID.");
                    }
                    catch (ApplicationException ex)
                    {
                        resultado.Errores.Add(prefijo + ex.Message);
                    }
                }
            }
        }

        public List<BEBE> listarBebes()
        {
            return repositorioBebe.listarBebes();
        }

        public List<SALA> listarSalas()
        {
            return negSalas.listarSalasActivas();
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
