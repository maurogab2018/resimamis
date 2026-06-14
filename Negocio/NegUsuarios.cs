using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ResimamisBackend.Negocio
{
    public class NegUsuarios
    {
        private const string RolAdministrativa = "Administrativa";

        private readonly ApplicationDbContext db;
        private readonly EstadoRepositorio estadoRepositorio;
        private readonly UsuarioRepositorio usuarioRepositorio;
        private readonly VoluntariaRepositorio voluntariaRepositorio;

        public NegUsuarios()
        {
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
            usuarioRepositorio = new UsuarioRepositorio();
            voluntariaRepositorio = new VoluntariaRepositorio();
        }

        public bool EsAdministrativaPorDni(int dni)
        {
            var usuario = usuarioRepositorio.ObtenerPorDni(dni);
            if (usuario == null || !UsuarioRepositorio.EsUsuarioOperativo(usuario))
                return false;

            var rol = db.VOLUNTARIA
                .AsNoTracking()
                .Include(v => v.RolInfo)
                .Where(v => v.IdVoluntaria == usuario.IdVoluntaria)
                .Select(v => v.RolInfo != null ? v.RolInfo.Nombre : null)
                .FirstOrDefault();

            return string.Equals(rol, RolAdministrativa, StringComparison.OrdinalIgnoreCase);
        }

        private void RequiereAdministrativa(int dniSolicitante)
        {
            if (!EsAdministrativaPorDni(dniSolicitante))
                throw new ForbiddenException("No tiene permisos para esta operación.");
        }

        private static void ValidarContrasenaNueva(string? contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < 8 || contrasena.Length > 15)
                throw new ApplicationException("La contraseña debe tener entre 8 y 15 caracteres.");
        }

        // AUTH ref:M — NegUsuarios.Loguear: verificación BCrypt + emisión JWT; cambiar orden puede romper seguridad o UX.
        public RespuestaLogin Loguear(RequestLogin usuario)
        {
            if (usuario == null)
            {
                throw new ApplicationException("El usuario ingresado a registrar es nulo");
            }

            var usuarioLoguear = usuarioRepositorio.ObtenerPorDni(usuario.Dni);
            if (usuarioLoguear == null)
            {
                throw new UnauthorizedException("Contraseña o usuario incorrecto");
            }

            if (!UsuarioRepositorio.EsUsuarioOperativo(usuarioLoguear))
                throw new UnauthorizedException("Usuario no disponible.");

            var contrasenaEncriptada = usuarioLoguear.Contrasena;
            bool contrasenaValida = BCrypt.Net.BCrypt.Verify(usuario.Contrasena, contrasenaEncriptada);

            if (!contrasenaValida)
            {
                throw new UnauthorizedException("Contraseña o usuario incorrecto");
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

        public bool RegistrarUsuario(int dniSolicitante, USUARIO usuario)
        {
            RequiereAdministrativa(dniSolicitante);
            return RegistrarUsuarioInterno(usuario);
        }

        private bool RegistrarUsuarioInterno(USUARIO usuario)
        {
            if (usuario == null)
                throw new ApplicationException("El usuario ingresado a registrar es nulo");

            ValidarContrasenaNueva(usuario.Contrasena);

            if (usuario.IdVoluntaria <= 0)
                throw new ApplicationException("Debe asociar el usuario a una voluntaria.");

            if (!db.VOLUNTARIA.AsNoTracking().Any(v => v.IdVoluntaria == usuario.IdVoluntaria))
                throw new ApplicationException("Voluntaria inexistente con ese IdVoluntaria.");

            if (usuarioRepositorio.VoluntariaTieneUsuarioActivo(usuario.IdVoluntaria))
                throw new ConflictException("La voluntaria ya tiene un usuario asociado.");

            if (usuarioRepositorio.ExisteUsuarioOperativoConDni(usuario.Dni))
                throw new ConflictException("Usuario con ese Dni ya creado");

            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);
            if (usuario.FechaCreacion == default)
                usuario.FechaCreacion = DateTime.UtcNow;

            usuario.idEstado = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creado", "Usuarios");

            db.USUARIO.Add(usuario);
            db.SaveChanges();
            return true;
        }

        public List<UsuarioListado> ListarUsuarios(int dniSolicitante)
        {
            RequiereAdministrativa(dniSolicitante);
            return usuarioRepositorio.ListarUsuariosOperativos();
        }

        public List<VOLUNTARIA> ListarVoluntariasSinUsuario(int dniSolicitante)
        {
            RequiereAdministrativa(dniSolicitante);
            return voluntariaRepositorio.listarVoluntariasSinUsuario();
        }

        public object ConsultarUsuarioPorId(int idUsuario, int dniSolicitante)
        {
            var u = usuarioRepositorio.ObtenerPorId(idUsuario, asNoTracking: true);
            if (u == null)
                throw new NotFoundException("Usuario no existente con ese Id");
            if (UsuarioRepositorio.EsUsuarioEliminado(u))
                throw new ApplicationException("Usuario no disponible.");

            var esAdmin = EsAdministrativaPorDni(dniSolicitante);
            var propio = u.Dni == dniSolicitante;
            if (!esAdmin && !propio)
                throw new ForbiddenException("No tiene permisos para consultar este usuario.");

            return new
            {
                u.IdUsuario,
                u.Dni,
                u.IdVoluntaria,
                u.FechaCreacion,
                u.idEstado
            };
        }

        // AUTH ref:M — CambiarContrasena: verificación BCrypt de contraseña actual antes de rehash.
        public bool CambiarContrasena(int dniSolicitante, RequestCambiarContrasena datos)
        {
            if (datos == null)
                throw new ApplicationException("Datos inválidos.");
            if (string.IsNullOrWhiteSpace(datos.ContrasenaActual))
                throw new ApplicationException("La contraseña actual es obligatoria.");

            ValidarContrasenaNueva(datos.ContrasenaNueva);

            var existente = usuarioRepositorio.ObtenerPorDni(dniSolicitante, asNoTracking: false);
            if (existente == null)
                throw new ApplicationException("Usuario no existente.");
            if (UsuarioRepositorio.EsUsuarioEliminado(existente))
                throw new ApplicationException("Usuario no disponible.");

            if (!BCrypt.Net.BCrypt.Verify(datos.ContrasenaActual, existente.Contrasena))
                throw new UnauthorizedException("La contraseña actual es incorrecta.");

            existente.Contrasena = BCrypt.Net.BCrypt.HashPassword(datos.ContrasenaNueva);
            usuarioRepositorio.GuardarCambios();
            return true;
        }

        public bool ModificarUsuario(int dniSolicitante, int idUsuario, USUARIO datos)
        {
            RequiereAdministrativa(dniSolicitante);

            if (datos == null)
                throw new ApplicationException("Datos inválidos.");

            var existente = usuarioRepositorio.ObtenerPorId(idUsuario, asNoTracking: false);
            if (existente == null)
                throw new NotFoundException("Usuario no existente con ese Id");
            if (UsuarioRepositorio.EsUsuarioEliminado(existente))
                throw new ApplicationException("No se puede modificar un usuario dado de baja.");

            if (datos.Dni != existente.Dni && usuarioRepositorio.ExisteUsuarioOperativoConDni(datos.Dni, idUsuario))
                throw new ConflictException("Ya existe otro usuario con ese Dni.");

            if (datos.IdVoluntaria != existente.IdVoluntaria)
            {
                if (!db.VOLUNTARIA.AsNoTracking().Any(v => v.IdVoluntaria == datos.IdVoluntaria))
                    throw new ApplicationException("Voluntaria inexistente con ese IdVoluntaria.");
                if (usuarioRepositorio.VoluntariaTieneUsuarioActivo(datos.IdVoluntaria, idUsuario))
                    throw new ConflictException("La voluntaria ya tiene un usuario asociado.");
            }

            existente.Dni = datos.Dni;
            existente.IdVoluntaria = datos.IdVoluntaria;

            if (!string.IsNullOrWhiteSpace(datos.Contrasena))
            {
                ValidarContrasenaNueva(datos.Contrasena);
                existente.Contrasena = BCrypt.Net.BCrypt.HashPassword(datos.Contrasena);
            }

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

        public bool EliminarUsuario(int dniSolicitante, int idUsuario)
        {
            RequiereAdministrativa(dniSolicitante);
            return usuarioRepositorio.EliminarLogico(idUsuario);
        }

        // AUTH ref:H — GenerateJwtToken: firma JWT; no alterar clave ni claims sin revisión explícita.
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
