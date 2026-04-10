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

        // ─── Catálogos simples (tabla propia) ─────────────────────────────
        private static readonly Dictionary<string, string> _tablas = new()
        {
            { "tipo-proceso",       "cat_tipoproceso_produccion" },
            { "presentacion",       "cat_presentacion_produccion" },
            { "bloque-insumos",     "cat_bloqueinsumos_produccion" },
            { "subproducto",        "cat_subproducto_produccion" },
            { "desecho",            "cat_desecho_produccion" },
            { "calibres-analisis",  "cat_calibres_analisis_produccion" },
        };

        // ─── Calibres: reutiliza calibres_catalogo con filtros ────────────
        // tipo → (grano_id, clasificacion | null para frijol)
        private static readonly Dictionary<string, (string GranoNombre, string? Clasificacion)> _calibreMap = new()
        {
            { "calibre-oz-am",  ("Garbanzo", "OZ AM") },
            { "calibre-oz-esp", ("Garbanzo", "OZ ESP") },
            { "calibre-frijol", ("Frijol",   null) },
        };

        // ─── GET ──────────────────────────────────────────────────────────
        [HttpGet("{tipo}")]
        public async Task<IActionResult> Listar(string tipo, [FromQuery] int sedeId = 0)
        {
            // Calibres: query especial sobre calibres_catalogo
            if (_calibreMap.TryGetValue(tipo, out var cmap))
            {
                var sql = cmap.Clasificacion != null
                    ? @"SELECT c.id, c.calibre_mm AS nombre, c.activo
                        FROM dbo.calibres_catalogo c
                        JOIN dbo.granos_catalogo g ON g.id = c.grano_id
                        WHERE g.nombre = @GranoNombre
                          AND c.clasificacion = @Clasificacion
                          AND c.activo = 1
                          AND (@SedeId = 0 OR c.sede_id = @SedeId)
                        ORDER BY c.id"
                    : @"SELECT c.id, c.calibre_mm AS nombre, c.activo
                        FROM dbo.calibres_catalogo c
                        JOIN dbo.granos_catalogo g ON g.id = c.grano_id
                        WHERE g.nombre = @GranoNombre
                          AND c.activo = 1
                          AND (@SedeId = 0 OR c.sede_id = @SedeId)
                        ORDER BY c.id";

                var rows = await _db.QueryAsync(sql,
                    new { cmap.GranoNombre, cmap.Clasificacion, SedeId = sedeId });
                return Ok(rows);
            }

            // Silos: reutiliza silos_calibre_catalogo
            if (tipo == "silo")
            {
                var rows = await _db.QueryAsync(
                    @"SELECT id, nombre, activo
                      FROM dbo.silos_calibre_catalogo
                      WHERE activo = 1
                        AND (@SedeId = 0 OR sede_id = @SedeId)
                      ORDER BY CAST(nombre AS NVARCHAR(4000))",
                    new { SedeId = sedeId });
                return Ok(rows);
            }

            // Catálogos simples
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            var orderBy = tipo == "calibres-analisis"
                ? "ORDER BY CASE WHEN nombre LIKE '%-%' THEN 1 ELSE 0 END, LEN(nombre), nombre"
                : "ORDER BY nombre";

            var data = await _db.QueryAsync(
                $"SELECT id, nombre, activo FROM dbo.{tabla} WHERE activo = 1 {orderBy}");
            return Ok(data);
        }

        // ─── POST ─────────────────────────────────────────────────────────
        [HttpPost("{tipo}")]
        public async Task<IActionResult> Agregar(string tipo, [FromBody] CatalogoReq dto,
            [FromQuery] int sedeId = 0)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest("El nombre es requerido");

            // Calibres → insertar en calibres_catalogo
            if (_calibreMap.TryGetValue(tipo, out var cmap))
            {
                var granoId = await _db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT id FROM dbo.granos_catalogo WHERE nombre = @nombre AND (@sedeId = 0 OR sede_id = @sedeId)",
                    new { nombre = cmap.GranoNombre, sedeId });

                if (granoId == null)
                    return BadRequest($"Grano '{cmap.GranoNombre}' no encontrado en la sede");

                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.calibres_catalogo
                        (calibre_mm, descuento_default_kg_ton, clasificacion, activo, sede_id, grano_id)
                      VALUES (@Nombre, 0, @Clasificacion, 1, @SedeId, @GranoId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { dto.Nombre, cmap.Clasificacion, SedeId = sedeId, GranoId = granoId });

                return Ok(new { id, nombre = dto.Nombre, activo = true });
            }

            // Silos → insertar en silos_calibre_catalogo
            if (tipo == "silo")
            {
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.silos_calibre_catalogo (nombre, activo, sede_id)
                      VALUES (@Nombre, 1, @SedeId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { dto.Nombre, SedeId = sedeId });

                return Ok(new { id, nombre = dto.Nombre, activo = true });
            }

            // Catálogos simples
            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            var newId = await _db.QuerySingleAsync<int>(
                $"INSERT INTO dbo.{tabla} (nombre) VALUES (@Nombre); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { dto.Nombre });

            return Ok(new { id = newId, dto.Nombre, activo = true });
        }

        // ─── PUT ──────────────────────────────────────────────────────────
        [HttpPut("{tipo}/{id}")]
        public async Task<IActionResult> Actualizar(string tipo, int id, [FromBody] CatalogoReq dto)
        {
            if (_calibreMap.ContainsKey(tipo))
            {
                await _db.ExecuteAsync(
                    "UPDATE dbo.calibres_catalogo SET calibre_mm = @Nombre, updated_at = GETDATE() WHERE id = @Id",
                    new { dto.Nombre, Id = id });
                return Ok();
            }

            if (tipo == "silo")
            {
                await _db.ExecuteAsync(
                    "UPDATE dbo.silos_calibre_catalogo SET nombre = @Nombre, updated_at = GETDATE() WHERE id = @Id",
                    new { dto.Nombre, Id = id });
                return Ok();
            }

            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            await _db.ExecuteAsync(
                $"UPDATE dbo.{tabla} SET nombre = @Nombre, activo = @Activo WHERE id = @Id",
                new { dto.Nombre, dto.Activo, Id = id });

            return Ok();
        }

        // ─── DELETE ───────────────────────────────────────────────────────
        [HttpDelete("{tipo}/{id}")]
        public async Task<IActionResult> Eliminar(string tipo, int id)
        {
            if (_calibreMap.ContainsKey(tipo))
            {
                await _db.ExecuteAsync("DELETE FROM dbo.calibres_catalogo WHERE id = @Id", new { Id = id });
                return Ok();
            }

            if (tipo == "silo")
            {
                await _db.ExecuteAsync("DELETE FROM dbo.silos_calibre_catalogo WHERE id = @Id", new { Id = id });
                return Ok();
            }

            if (!_tablas.TryGetValue(tipo, out var tabla))
                return BadRequest("Tipo de catálogo inválido");

            await _db.ExecuteAsync($"DELETE FROM dbo.{tabla} WHERE id = @Id", new { Id = id });
            return Ok();
        }

        public class CatalogoReq
        {
            public string Nombre { get; set; } = "";
            public bool Activo { get; set; } = true;
        }
    }
}
