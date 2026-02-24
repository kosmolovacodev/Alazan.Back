using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/pantallas")]
    [Route("pantallas")]

    public class PantallasController : ControllerBase
    {
        private readonly IDbConnection _db;

        public PantallasController(IDbConnection db)
        {
            _db = db;
        }

        // Obtener todas las pantallas del catálogo (filtradas por sede)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int sede_id = 0)
        {
            try
            {
                var sql = @"SELECT id, nombre_pantalla, descripcion FROM dbo.pantallas
                            WHERE @sede_id = 0 OR sede_id = @sede_id OR sede_id IS NULL";
                var pantallas = await _db.QueryAsync(sql, new { sede_id });
                return Ok(pantallas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener pantallas: {ex.Message}");
            }
        }

        // Agregar una nueva pantalla al catálogo
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] dynamic p)
        {
            try
            {
                var el = (System.Text.Json.JsonElement)p;
                int? sedeId = el.TryGetProperty("sede_id", out var sedeProp) && sedeProp.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? sedeProp.GetInt32()
                    : (int?)null;

                var sql = "INSERT INTO dbo.pantallas (nombre_pantalla, descripcion, sede_id) VALUES (@nombre, @desc, @sedeId)";

                await _db.ExecuteAsync(sql, new {
                    nombre = el.GetProperty("nombre_pantalla").GetString(),
                    desc = el.GetProperty("descripcion").GetString(),
                    sedeId
                });

                return Ok(new { message = "Pantalla registrada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al registrar pantalla: {ex.Message}");
            }
        }

        // Eliminar una pantalla del catálogo
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) 
        {
            try
            {
                await _db.ExecuteAsync("DELETE FROM dbo.pantallas WHERE id = @id", new { id });
                return Ok(new { message = "Pantalla eliminada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar: {ex.Message}");
            }
        }
    }
}