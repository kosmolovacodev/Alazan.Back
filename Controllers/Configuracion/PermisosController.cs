using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/permisos")]
    [Route("permisos")]

    public class PermisosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public PermisosController(IDbConnection db) => _db = db;

        // GET: api/permisos/2
        [HttpGet("{rolId}")]
        public async Task<IActionResult> GetPermisos(int rolId) 
        {
            try 
            {
                // Buscamos directamente en la tabla roles la columna permisos_json
                var sql = "SELECT permisos_json FROM dbo.roles WHERE id = @rolId";
                var resultado = await _db.QueryFirstOrDefaultAsync<string>(sql, new { rolId });
                
                // Si el resultado es nulo, devolvemos un string de array vacío "[]"
                return Ok(resultado ?? "[]");
            }
            catch (Exception ex) 
            {
                return StatusCode(500, $"Error al obtener permisos: {ex.Message}");
            }
        }

        // POST: api/permisos/2
        // Este es el método que tu Front está llamando: api.post(`/api/permisos/${rolSeleccionado.value}`)
        [HttpPost("{rolId}")]
        public async Task<IActionResult> UpdatePermisos(int rolId, [FromBody] System.Text.Json.JsonElement body) 
        {
            try 
            {
                // Extraemos el string JSON que envía el frontend { permisos: "['Pantalla1', 'Pantalla2']" }
                if (!body.TryGetProperty("permisos", out var permisosProperty))
                {
                    return BadRequest("El campo 'permisos' es requerido.");
                }

                string jsonPermisos = permisosProperty.GetString() ?? "[]";

                // Actualizamos la columna permisos_json en el rol correspondiente
                var sql = "UPDATE dbo.roles SET permisos_json = @jsonPermisos WHERE id = @rolId";
                
                await _db.ExecuteAsync(sql, new { jsonPermisos, rolId });
                
                return Ok(new { message = "Permisos actualizados correctamente" });
            }
            catch (Exception ex) 
            {
                return StatusCode(500, $"Error al actualizar permisos: {ex.Message}");
            }
        }
    }
}