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
        public NegUsuarios()
        {
            db = new ApplicationDbContext();
        }

        public RespuestaLogin Loguear(RequestLogin usuario)
        {
            if (usuario == null)
            {
                throw new ApplicationException("El usuario ingresado a registrar es nulo");
            }

            var usuarioLoguear = db.USUARIO.Where(u => u.Dni == usuario.Dni).FirstOrDefault();
            if (usuarioLoguear == null)
            {
                throw new ApplicationException("Contraseña o usuario incorrecto");
            }

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
            //validar que existe ese dni y la voluntaria ya no tengo otro usuario
            if ( usuario.Contrasena.Length > 15 || usuario.Contrasena == null)
            {
                throw new ApplicationException("Revise el largo de los datos ingreados, contraseña de minimo 8 caracteres, recorda completar campos obligatorios");
            }
            if (db.USUARIO.Where(u => u.Dni == usuario.Dni).FirstOrDefault() != null)
            {
                throw new ApplicationException("Usuario con ese Dni ya creado");
            }
            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);

            db.USUARIO.Add(usuario);
            db.SaveChanges();
            return true;
        }
        private string GenerateJwtToken(RequestLogin user)
        {
            // Configurar los claims del token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Dni.ToString()),
                new Claim(ClaimTypes.Name, user.Dni.ToString()),
                // Agrega otros claims según tus necesidades
            };

            // Generar la clave secreta
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("b5c3a9d1e8f0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c")); // Reemplaza con tu clave secreta
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Configurar la descripción del token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30), // Configura la fecha de expiración del token
                SigningCredentials = credentials
            };

            // Generar el token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Devolver el token como string
            return tokenHandler.WriteToken(token);
        }
    }
}
