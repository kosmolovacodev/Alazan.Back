using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    /// <summary>
    /// CRUD para los catálogos del módulo Bodega.
    /// Soporta dos familias:
    ///   – Con clave: bodega, cuadrante  → {id, clave, nombre}
    ///   – Simples:   tipo-costal, subproducto-bodega → {id, nombre}
    /// </summary>
    [ApiController]
    [Route("bodega-catalogos")]
    public class BodegaCatalogosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public BodegaCatalogosController(IDbConnection db) => _db = db;

        // Tablas que tienen campo clave
        private static readonly Dictionary<string, string> _conClave = new()
        {
            { "bodega",    "cat_bodega"    },
            { "cuadrante", "cat_cuadrante" },
        };

        // Tablas que solo tienen nombre
        private static readonly Dictionary<string, string> _simples = new()
        {
            { "tipo-costal",        "cat_tipo_costal"        },
            { "subproducto-bodega", "cat_subproducto_bodega" },
        };

        // ─── GET ──────────────────────────────────────────────────────────
        [HttpGet("{tipo}")]
        public async Task<IActionResult> Listar(string tipo, [FromQuery] int sedeId = 0)
        {
            if (_conClave.TryGetValue(tipo, out var tbl))
            {
                var rows = await _db.QueryAsync(
                    $@"SELECT id, clave, nombre, activo
                       FROM dbo.{tbl}
                       WHERE activo = 1
                         AND (@SedeId = 0 OR sede_id = @SedeId)
                       ORDER BY clave",
                    new { SedeId = sedeId });
                return Ok(rows);
            }

            if (_simples.TryGetValue(tipo, out var tbl2))
            {
                var rows = await _db.QueryAsync(
                    $@"SELECT id, nombre, activo
                       FROM dbo.{tbl2}
                       WHERE activo = 1
                         AND (@SedeId = 0 OR sede_id = @SedeId)
                       ORDER BY nombre",
                    new { SedeId = sedeId });
                return Ok(rows);
            }

            return BadRequest(new { message = "Tipo de catálogo inválido" });
        }

        // ─── POST ─────────────────────────────────────────────────────────
        [HttpPost("{tipo}")]
        public async Task<IActionResult> Agregar(
            string tipo,
            [FromBody] BodegaCatRequest dto,
            [FromQuery] int sedeId = 0)
        {
            try
            {
                if (_conClave.TryGetValue(tipo, out var tbl))
                {
                    if (string.IsNullOrWhiteSpace(dto.Clave))
                        return BadRequest(new { message = "La clave es obligatoria" });

                    var id = await _db.QuerySingleAsync<int>(
                        $@"INSERT INTO dbo.{tbl} (sede_id, clave, nombre, activo)
                           VALUES (@SedeId, @Clave, @Nombre, 1);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new { SedeId = sedeId, dto.Clave, dto.Nombre });

                    return Ok(new { id });
                }

                if (_simples.TryGetValue(tipo, out var tbl2))
                {
                    var id = await _db.QuerySingleAsync<int>(
                        $@"INSERT INTO dbo.{tbl2} (sede_id, nombre, activo)
                           VALUES (@SedeId, @Nombre, 1);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new { SedeId = sedeId, dto.Nombre });

                    return Ok(new { id });
                }

                return BadRequest(new { message = "Tipo inválido" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al agregar", error = ex.Message });
            }
        }

        // ─── PUT ──────────────────────────────────────────────────────────
        [HttpPut("{tipo}/{id}")]
        public async Task<IActionResult> Actualizar(
            string tipo, int id,
            [FromBody] BodegaCatRequest dto)
        {
            try
            {
                if (_conClave.TryGetValue(tipo, out var tbl))
                {
                    await _db.ExecuteAsync(
                        $"UPDATE dbo.{tbl} SET clave = @Clave, nombre = @Nombre WHERE id = @Id",
                        new { dto.Clave, dto.Nombre, Id = id });
                    return Ok();
                }

                if (_simples.TryGetValue(tipo, out var tbl2))
                {
                    await _db.ExecuteAsync(
                        $"UPDATE dbo.{tbl2} SET nombre = @Nombre WHERE id = @Id",
                        new { dto.Nombre, Id = id });
                    return Ok();
                }

                return BadRequest(new { message = "Tipo inválido" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
            }
        }

        // ─── DELETE (soft delete) ─────────────────────────────────────────
        [HttpDelete("{tipo}/{id}")]
        public async Task<IActionResult> Eliminar(string tipo, int id)
        {
            string? tabla = null;
            if (_conClave.TryGetValue(tipo, out var t1)) tabla = t1;
            else if (_simples.TryGetValue(tipo, out var t2)) tabla = t2;

            if (tabla == null)
                return BadRequest(new { message = "Tipo inválido" });

            await _db.ExecuteAsync(
                $"UPDATE dbo.{tabla} SET activo = 0 WHERE id = @Id",
                new { Id = id });

            return Ok();
        }
    }

    public class BodegaCatRequest
    {
        public string? Clave  { get; set; }
        public string  Nombre { get; set; } = "";
    }
}
