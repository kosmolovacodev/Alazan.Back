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

        // Obtener todas las pantallas del catálogo
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var sql = "SELECT id, nombre_pantalla, descripcion FROM dbo.pantallas";
                var pantallas = await _db.QueryAsync(sql);
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
                var sql = "INSERT INTO dbo.pantallas (nombre_pantalla, descripcion) VALUES (@nombre, @desc)";
                
                await _db.ExecuteAsync(sql, new { 
                    nombre = el.GetProperty("nombre_pantalla").GetString(),
                    desc = el.GetProperty("descripcion").GetString() 
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