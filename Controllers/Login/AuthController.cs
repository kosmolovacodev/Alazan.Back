using Alazan.API.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly IConfiguration _config;

        // Inyectamos IDbConnection para Dapper y IConfiguration para leer appsettings.json
        public AuthController(IDbConnection db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // 1. Buscamos el usuario por Email incluyendo el hash de la contraseña
                var sql = @"
                    SELECT 
                        au.id, 
                        au.email, 
                        au.encrypted_password, 
                        u.nombre_completo, 
                        u.activo,
                        r.nombre_rol, 
                        r.permisos_json,
                        u.sede_id
                    FROM auth.users au
                    INNER JOIN dbo.usuarios u ON au.id = u.auth_user_id
                    INNER JOIN dbo.roles r ON u.rol_id = r.id
                    WHERE au.email = @Email";

                var user = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = request.Email });

                // 2. Validación de existencia
                if (user == null) 
                    return Unauthorized(new { message = "Credenciales incorrectas" });

                // 3. Validación de estado activo
                if (user.activo.ToString().ToLower() == "false" || user.activo.ToString() == "0")
                {
                    return Unauthorized(new { message = "Usuario inactivo" });
                }

                // 4. Verificación de contraseña con BCrypt
                bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.encrypted_password);

                if (!isValid)
                    return Unauthorized(new { message = "Credenciales incorrectas" });

                // 5. GENERACIÓN DEL JWT (SESIÓN) 
                var token = GenerarTokenJWT(user);

                // 6. Respuesta al Front (incluye el token y datos del usuario)
                var userResponse = new {
                    id = user.id,
                    email = user.email,
                    nombre_completo = user.nombre_completo,
                    nombre_rol = user.nombre_rol,
                    permisos_json = user.permisos_json,
                    sede_id = user.sede_id
                };

                return Ok(new { 
                    jwt = token, 
                    user = userResponse 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en el servidor", detail = ex.Message });
            }
        }

        private string GenerarTokenJWT(dynamic user)
        {
            // 1. Validación de seguridad para la Key
            var secretKey = _config.GetSection("Jwt")["Key"];
            if (string.IsNullOrEmpty(secretKey)) 
                throw new Exception("La clave JWT no está configurada en appsettings.json");

            var key = Encoding.UTF8.GetBytes(secretKey);

            // 2. Definir los datos del usuario (Claims)
            // Usamos Convert.ToString para asegurar que dynamic no falle
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Convert.ToString(user.id)),
                new Claim(JwtRegisteredClaimNames.Email, Convert.ToString(user.email)),
                new Claim("nombre", Convert.ToString(user.nombre_completo)),
                new Claim("sede_id", Convert.ToString(user.sede_id)),
                new Claim(ClaimTypes.Role, Convert.ToString(user.nombre_rol))
            };

            // 3. Configurar el descriptor del token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _config.GetSection("Jwt")["Issuer"],
                Audience = _config.GetSection("Jwt")["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var createdToken = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(createdToken);
        }
    }

    public record LoginRequest(string Email, string Password);
}