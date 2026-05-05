using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResimamisBackend.Datos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ResimamisBackend.Negocio
{
    public class NegUsuarios
    {
        private readonly ApplicationDbContext db;
        private readonly EstadoRepositorio estadoRepositorio;
        private readonly UsuarioRepositorio usuarioRepositorio;

        public NegUsuarios()
        {
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
            usuarioRepositorio = new UsuarioRepositorio();
        }

        public RespuestaLogin Loguear(RequestLogin usuario)
        {
            if (usuario == null)
            {
                throw new ApplicationException("El usuario ingresado a registrar es nulo");
            }

            var usuarioLoguear = usuarioRepositorio.ObtenerPorDni(usuario.Dni);
            if (usuarioLoguear == null)
            {
                throw new ApplicationException("Contraseña o usuario incorrecto");
            }

            if (!UsuarioRepositorio.EsUsuarioOperativo(usuarioLoguear))
                throw new ApplicationException("Usuario no disponible.");

            var contrasenaEncriptada = usuarioLoguear.Contrasena;
            bool contrasenaValida = BCrypt.Net.BCrypt.Verify(usuario.Contrasena, contrasenaEncriptada);

            if (!contrasenaValida)
            {
                throw new ApplicationException("Contraseña o usuario incorrecto");
            }
            var tokenDevolver = GenerateJwtToken(usuario);
            var voluntariaUsuario = db.VOLUNTARIA.Include(v => v.RolInfo).Single(v => v.IdVoluntaria == usuarioLoguear.IdVoluntaria);
            return new RespuestaLogin()
            {
                Token = tokenDevolver,
                Resultado = "Exito",
                Voluntaria = voluntariaUsuario
            };
        }

        public bool RegistrarUsuario(USUARIO usuario)
        {
            if (usuario == null)
            {
                throw new ApplicationException("El usuario ingresado a registrar es nulo");
            }
            if (usuario.Contrasena.Length > 15 || usuario.Contrasena == null)
            {
                throw new ApplicationException("Revise el largo de los datos ingreados, contraseña de minimo 8 caracteres, recorda completar campos obligatorios");
            }
            if (db.USUARIO.Where(u => u.Dni == usuario.Dni).FirstOrDefault() != null)
            {
                throw new ApplicationException("Usuario con ese Dni ya creado");
            }
            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);
            if (usuario.FechaCreacion == default)
                usuario.FechaCreacion = DateTime.UtcNow;

            usuario.idEstado = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creado", "Usuarios");

            db.USUARIO.Add(usuario);
            db.SaveChanges();
            return true;
        }

        public object ConsultarUsuarioPorId(int idUsuario)
        {
            var u = usuarioRepositorio.ObtenerPorId(idUsuario, asNoTracking: true);
            if (u == null)
                throw new ApplicationException("Usuario no existente con ese Id");
            if (UsuarioRepositorio.EsUsuarioEliminado(u))
                throw new ApplicationException("Usuario no disponible.");
            return new
            {
                u.IdUsuario,
                u.Dni,
                u.IdVoluntaria,
                u.FechaCreacion,
                u.idEstado
            };
        }

        public bool ModificarUsuario(int idUsuario, USUARIO datos)
        {
            if (datos == null)
                throw new ApplicationException("Datos inválidos.");

            var existente = usuarioRepositorio.ObtenerPorId(idUsuario, asNoTracking: false);
            if (existente == null)
                throw new ApplicationException("Usuario no existente con ese Id");
            if (UsuarioRepositorio.EsUsuarioEliminado(existente))
                throw new ApplicationException("No se puede modificar un usuario dado de baja.");

            if (existente.Dni != datos.Dni && usuarioRepositorio.ExisteOtroUsuarioConDni(datos.Dni, idUsuario))
                throw new ApplicationException("Ya existe otro usuario con ese Dni.");

            existente.Dni = datos.Dni;
            existente.IdVoluntaria = datos.IdVoluntaria;

            if (!string.IsNullOrWhiteSpace(datos.Contrasena))
                existente.Contrasena = BCrypt.Net.BCrypt.HashPassword(datos.Contrasena);

            if (datos.idEstado.HasValue)
            {
                var valido = db.ESTADO
                    .AsNoTracking()
                    .Include(e => e.ambito)
                    .Any(e =>
                        e.idEstado == datos.idEstado.Value
                        && e.ambito.nombre == "Usuarios"
                        && e.nombre != "Eliminado");
                if (!valido)
                    throw new ApplicationException("El estado no es válido para el ámbito Usuarios (no use Eliminado aquí).");
                existente.idEstado = datos.idEstado;
            }

            usuarioRepositorio.GuardarCambios();
            return true;
        }

        public bool EliminarUsuario(int idUsuario)
        {
            return usuarioRepositorio.EliminarLogico(idUsuario);
        }

        private string GenerateJwtToken(RequestLogin user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Dni.ToString()),
                new Claim(ClaimTypes.Name, user.Dni.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("b5c3a9d1e8f0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
