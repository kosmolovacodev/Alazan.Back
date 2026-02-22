using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;


namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("roles")]
public class RolesController : ControllerBase
{
    private readonly IDbConnection _db;
    public RolesController(IDbConnection db) => _db = db;

    [HttpGet]
        public async Task<IActionResult> GetRoles([FromQuery] int sedeId)
        {
            // Admin global (sedeId=0) ve todos los roles
            // Usuarios normales ven: roles de su sede + roles globales (sede_id=0 o NULL)
            var sql = @"SELECT id, nombre_rol, descripcion, permisos_json, activo
                        FROM dbo.roles
                        WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost]
            public async Task<IActionResult> AddRol([FromBody] dynamic r, [FromQuery] int sedeId)
            {
                try
                {
                    var el = (System.Text.Json.JsonElement)r;

                    if (!el.TryGetProperty("nombre_rol", out var nombreElem)) {
                        return BadRequest("El campo 'nombre_rol' es requerido.");
                    }

                    string nombre = nombreElem.GetString();

                    // --- NUEVA VALIDACIÓN ---
                    // Buscamos si ya existe ese nombre ESPECÍFICAMENTE para esa sedeId
                    var existe = await _db.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(1) FROM dbo.roles WHERE nombre_rol = @nombre AND sede_id = @sedeId", 
                        new { nombre, sedeId });

                    if (existe > 0) {
                        return BadRequest(new { message = $"El rol '{nombre}' ya existe para esta sede." });
                    }
                    // -------------------------

                    string desc = el.TryGetProperty("descripcion", out var descElem) ? descElem.GetString() ?? "" : "";

                    var sql = @"INSERT INTO dbo.roles (nombre_rol, descripcion, activo, created_at, sede_id)
                                VALUES (@nombre, @desc, 1, SYSDATETIMEOFFSET(), @sedeId)";

                    await _db.ExecuteAsync(sql, new { nombre, desc, sedeId });

                    return Ok(new { message = "Rol creado" });
                }
                catch (Exception ex)
                {
                    // Si no ejecutaste el cambio en SQL, el catch atrapará el error de "Duplicate Key" aquí
                    return StatusCode(500, $"Error: {ex.Message}");
                }
            }
            

        [HttpPut("{id}")]
            public async Task<IActionResult> UpdateRol(long id, [FromBody] RolUpdateRequest r) 
            {
                try 
                {
                    // Actualizamos tanto el nombre como la descripción
                    var sql = @"UPDATE dbo.roles 
                                SET nombre_rol = @nombre_rol, 
                                    descripcion = @descripcion, 
                                    updated_at = SYSDATETIMEOFFSET() 
                                WHERE id = @id";
                    
                    await _db.ExecuteAsync(sql, new { 
                        nombre_rol = r.Nombre_rol,
                        descripcion = r.Descripcion,
                        id 
                    });
                    
                    return Ok();
                }
                catch (Exception ex) 
                { 
                    return StatusCode(500, $"Error: {ex.Message}"); 
                }
            }

            // Clase de apoyo para recibir los datos correctamente
            public class RolUpdateRequest {
                public string Nombre_rol { get; set; }
                public string Descripcion { get; set; }
            }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRol(long id) 
        {
            try 
            {
                // Protección: No borrar el ADMIN (usualmente ID 1)
                if (id == 1) return BadRequest("El rol Administrador está protegido.");

                await _db.ExecuteAsync("DELETE FROM dbo.roles WHERE id = @id", new { id });
                return Ok();
            }
            catch (Exception ex) { return StatusCode(500, $"Error: {ex.Message}"); }
        }
}
}