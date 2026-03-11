using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("produccion-catalogos")]
    public class ProduccionCatalogosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public ProduccionCatalogosController(IDbConnection db) => _db = db;

        // Mapeo tipo → tabla
        private static readonly Dictionary<string, string> _tablas = new()
        {
            { "tipo-proceso",    "cat_tipoproceso_produccion" },
            { "presentacion",    "cat_presentacion_produccion" },
            { "bloque-insumos",  "cat_bloqueinsumos_produccion" },
            { "subproducto",     "cat_subproducto_produccion" },
            { "desecho",         "cat_desecho_produccion" },
        };

        [HttpGet("{tipo}")]
        public async Task<IActionResult> Listar(string tipo)
        {
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            var rows = await _db.QueryAsync(
                $"SELECT id, nombre, activo FROM dbo.{tabla} ORDER BY nombre");
            return Ok(rows);
        }

        [HttpPost("{tipo}")]
        public async Task<IActionResult> Agregar(string tipo, [FromBody] CatalogoReq dto)
        {
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest("El nombre es requerido");

            var id = await _db.QuerySingleAsync<int>(
                $@"INSERT INTO dbo.{tabla} (nombre) VALUES (@Nombre);
                   SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { dto.Nombre });

            return Ok(new { id, dto.Nombre, activo = true });
        }

        [HttpPut("{tipo}/{id}")]
        public async Task<IActionResult> Actualizar(string tipo, int id, [FromBody] CatalogoReq dto)
        {
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            await _db.ExecuteAsync(
                $"UPDATE dbo.{tabla} SET nombre = @Nombre, activo = @Activo, fecha_update = SYSDATETIMEOFFSET() WHERE id = @Id",
                new { dto.Nombre, dto.Activo, Id = id });

            return Ok(new { message = "Actualizado" });
        }

        [HttpDelete("{tipo}/{id}")]
        public async Task<IActionResult> Eliminar(string tipo, int id)
        {
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            await _db.ExecuteAsync($"DELETE FROM dbo.{tabla} WHERE id = @Id", new { Id = id });
            return Ok(new { message = "Eliminado" });
        }

        public class CatalogoReq
        {
            public string Nombre { get; set; } = "";
            public bool Activo { get; set; } = true;
        }
    }
}
