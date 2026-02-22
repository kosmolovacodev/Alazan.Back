using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
// using BCrypt.Net;
using SistemaAlazan.Models;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public UsuariosController(IDbConnection db) => _db = db;

        // --- 1. OBTENER USUARIOS (UNIFICADO Y CORREGIDO) ---
        [HttpGet]
        public async Task<IActionResult> GetUsuarios([FromQuery] int sedeId) // Cambiado a FromQuery
        {
            // Lógica: Si sedeId es 0 (Admin Global), trae todos. 
            // Si es > 0, filtra estrictamente por esa sede.
            var sql = @"
                SELECT 
                    u.*, 
                    u.nombre_completo AS nombre, 
                    u.email AS username,
                    r.nombre_rol,
                    u.sede_id
                FROM dbo.usuarios u
                INNER JOIN auth.users a ON u.auth_user_id = a.id
                INNER JOIN dbo.roles r ON u.rol_id = r.id
                LEFT JOIN dbo.sedes_catalogo s ON u.sede_id = s.id
                WHERE (@sedeId = 0 OR u.sede_id = @sedeId)";
                
            var data = await _db.QueryAsync(sql, new { sedeId });
            return Ok(data);
        }

        // --- 2. PERFIL POR AUTH ID ---
        [HttpGet("perfil/{authId}")]
        public async Task<IActionResult> GetPerfilPorAuthId(Guid authId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        u.id, 
                        u.auth_user_id, 
                        u.nombre_completo, 
                        u.email, 
                        r.nombre_rol,
                        u.rol_id,
                        u.firma,
                        u.sede_id      
                    FROM dbo.usuarios u
                    INNER JOIN dbo.roles r ON u.rol_id = r.id
                    WHERE u.auth_user_id = @authId";

                var usuario = await _db.QueryFirstOrDefaultAsync(sql, new { authId });

                if (usuario == null)
                    return NotFound(new { message = "Usuario no encontrado o inactivo." });

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener perfil: {ex.Message}");
            }
        }

        // --- 3. AGREGAR USUARIO ---
        [HttpPost]
        public async Task<IActionResult> AddUsuario([FromBody] dynamic u)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var transaction = _db.BeginTransaction();
            try
            {
                var el = (System.Text.Json.JsonElement)u;
                string email = el.GetProperty("username").GetString();
                string passRaw = el.GetProperty("password").GetString(); 
                string nombre = el.GetProperty("nombre").GetString();
                int rolId = el.GetProperty("rol_id").GetInt32();
                bool activo = el.GetProperty("activo").GetBoolean();
                int sedeId = el.GetProperty("sede_id").GetInt32(); 
                
                string passHash = BCrypt.Net.BCrypt.HashPassword(passRaw);

                string firma = el.TryGetProperty("firma", out var f) ? f.GetString() : null;
                string depto = el.TryGetProperty("departamento", out var d) ? d.GetString() : null;
                string tel = el.TryGetProperty("telefono", out var t) ? t.GetString() : null;

                var sqlAuth = @"INSERT INTO auth.users (email, encrypted_password, created_at) 
                                OUTPUT INSERTED.id 
                                VALUES (@email, @passHash, SYSDATETIMEOFFSET())";
                
                var authId = await _db.QuerySingleAsync<Guid>(sqlAuth, new { email, passHash }, transaction);

                var sqlPerfil = @"INSERT INTO dbo.usuarios 
                    (auth_user_id, nombre_completo, email, rol_id, firma, departamento, telefono, activo, fecha_registro, sede_id) 
                    VALUES (@authId, @nombre, @email, @rolId, @firma, @depto, @tel, @activo, SYSDATETIMEOFFSET(), @sedeId)";
        
                await _db.ExecuteAsync(sqlPerfil, new { authId, nombre, email, rolId, firma, depto, tel, activo, sedeId }, transaction);

                transaction.Commit();
                return Ok(new { message = "Usuario creado con éxito" });
            }
            catch (Exception ex) { transaction.Rollback(); return StatusCode(500, $"Error: {ex.Message}"); }
            finally { _db.Close(); }
        }

        // --- 4. ACTUALIZAR USUARIO ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] dynamic u)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var transaction = _db.BeginTransaction();
            try
            {
                var el = (System.Text.Json.JsonElement)u;
                string nombre = el.GetProperty("nombre").GetString();
                string email = el.GetProperty("username").GetString();
                int rolId = el.GetProperty("rol_id").GetInt32();
                bool activo = el.GetProperty("activo").GetBoolean();
                string firma = el.TryGetProperty("firma", out var f) ? f.GetString() : null;
                string depto = el.TryGetProperty("departamento", out var d) ? d.GetString() : null;
                string tel = el.TryGetProperty("telefono", out var t) ? t.GetString() : null;
                int sedeId = el.GetProperty("sede_id").GetInt32(); 

                var sqlUpd = @"UPDATE dbo.usuarios 
                               SET nombre_completo = @nombre, email = @email, rol_id = @rolId, 
                                   firma = @firma, departamento = @depto, telefono = @tel, activo = @activo,
                                   sede_id = @sedeId,
                                   updated_at = SYSDATETIMEOFFSET()
                               WHERE id = @id";
        
                await _db.ExecuteAsync(sqlUpd, new { nombre, email, rolId, firma, depto, tel, activo, sedeId, id }, transaction);

                var authId = await _db.QueryFirstOrDefaultAsync<Guid>(
                    "SELECT auth_user_id FROM dbo.usuarios WHERE id = @id", new { id }, transaction);

                await _db.ExecuteAsync("UPDATE auth.users SET email = @email WHERE id = @authId", new { email, authId }, transaction);

                if (el.TryGetProperty("password", out var pElem) && !string.IsNullOrEmpty(pElem.GetString()))
                {
                    string newPassHash = BCrypt.Net.BCrypt.HashPassword(pElem.GetString());
                    await _db.ExecuteAsync("UPDATE auth.users SET encrypted_password = @newPassHash WHERE id = @authId", 
                        new { newPassHash, authId }, transaction);
                }

                transaction.Commit();
                return Ok(new { message = "Usuario actualizado" });
            }
            catch (Exception ex) { transaction.Rollback(); return StatusCode(500, $"Error: {ex.Message}"); }
            finally { _db.Close(); }
        }
    }
}