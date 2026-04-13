using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    /// <summary>
    /// CRUD genérico para los catálogos del módulo Instrucciones de Embarque:
    ///   presentacion | broker | lugar | plantilla
    /// Las primeras tres son simples (id, nombre).
    /// La plantilla tiene (id, titulo, cuerpo).
    /// </summary>
    [ApiController]
    [Route("ie-catalogos")]
    public class InstruccionesEmbarqueCatalogosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public InstruccionesEmbarqueCatalogosController(IDbConnection db) => _db = db;

        private static readonly Dictionary<string, string> _simples = new()
        {
            { "presentacion", "cat_ie_presentacion" },
            { "broker",       "cat_ie_broker"       },
            { "lugar",        "cat_ie_lugar"        },
        };

        // ─── GET (simples) ────────────────────────────────────────────
        [HttpGet("{tipo}")]
        public async Task<IActionResult> Listar(string tipo)
        {
            if (_simples.TryGetValue(tipo, out var tbl))
            {
                var rows = await _db.QueryAsync(
                    $"SELECT id, nombre FROM dbo.{tbl} WHERE activo = 1 ORDER BY nombre");
                return Ok(rows);
            }
            return BadRequest(new { message = "Tipo inválido" });
        }

        [HttpPost("{tipo}")]
        public async Task<IActionResult> Agregar(string tipo, [FromBody] IeCatRequest dto)
        {
            if (!_simples.TryGetValue(tipo, out var tbl))
                return BadRequest(new { message = "Tipo inválido" });
            try
            {
                var id = await _db.QuerySingleAsync<int>(
                    $@"INSERT INTO dbo.{tbl} (nombre, activo)
                       VALUES (@Nombre, 1);
                       SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { dto.Nombre });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{tipo}/{id}")]
        public async Task<IActionResult> Actualizar(string tipo, int id, [FromBody] IeCatRequest dto)
        {
            if (!_simples.TryGetValue(tipo, out var tbl))
                return BadRequest(new { message = "Tipo inválido" });
            await _db.ExecuteAsync(
                $"UPDATE dbo.{tbl} SET nombre = @Nombre WHERE id = @Id",
                new { dto.Nombre, Id = id });
            return Ok();
        }

        [HttpDelete("{tipo}/{id}")]
        public async Task<IActionResult> Eliminar(string tipo, int id)
        {
            if (!_simples.TryGetValue(tipo, out var tbl))
                return BadRequest(new { message = "Tipo inválido" });
            await _db.ExecuteAsync(
                $"UPDATE dbo.{tbl} SET activo = 0 WHERE id = @Id",
                new { Id = id });
            return Ok();
        }

        // ─── Plantillas (titulo + cuerpo) ─────────────────────────────

        [HttpGet("plantilla")]
        public async Task<IActionResult> ListarPlantillas()
        {
            var rows = await _db.QueryAsync(
                "SELECT id, titulo, cuerpo FROM dbo.cat_ie_plantilla WHERE activo = 1 ORDER BY titulo");
            return Ok(rows);
        }

        [HttpPost("plantilla")]
        public async Task<IActionResult> AgregarPlantilla([FromBody] IePlantillaRequest dto)
        {
            try
            {
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.cat_ie_plantilla (titulo, cuerpo, activo)
                      VALUES (@Titulo, @Cuerpo, 1);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { dto.Titulo, dto.Cuerpo });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("plantilla/{id}")]
        public async Task<IActionResult> ActualizarPlantilla(int id, [FromBody] IePlantillaRequest dto)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.cat_ie_plantilla SET titulo = @Titulo, cuerpo = @Cuerpo WHERE id = @Id",
                new { dto.Titulo, dto.Cuerpo, Id = id });
            return Ok();
        }

        [HttpDelete("plantilla/{id}")]
        public async Task<IActionResult> EliminarPlantilla(int id)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.cat_ie_plantilla SET activo = 0 WHERE id = @Id",
                new { Id = id });
            return Ok();
        }
    }

    public class IeCatRequest
    {
        public string Nombre { get; set; } = "";
    }

    public class IePlantillaRequest
    {
        public string Titulo { get; set; } = "";
        public string Cuerpo { get; set; } = "";
    }
}
