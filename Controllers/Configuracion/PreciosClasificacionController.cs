using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("precios-clasificacion")]
    public class PreciosClasificacionController : ControllerBase
    {
        private readonly IDbConnection _db;
        public PreciosClasificacionController(IDbConnection db) => _db = db;

        // GET /precios-clasificacion?sedeId=8&granoId=4
        [HttpGet]
        public async Task<IActionResult> GetClasificaciones([FromQuery] int sedeId, [FromQuery] int granoId)
        {
            var sql = @"
                SELECT
                    id        AS Id,
                    sede_id   AS SedeId,
                    grano_id  AS GranoId,
                    nombre    AS Nombre,
                    codigo    AS Codigo,
                    precio_kg AS PrecioKg,
                    activo    AS Activo,
                    orden     AS Orden
                FROM dbo.precios_clasificacion
                WHERE sede_id = @sedeId AND grano_id = @granoId
                ORDER BY orden";

            var rows = await _db.QueryAsync<PrecioClasificacionDto>(sql, new { sedeId, granoId });
            return Ok(rows);
        }

        // PUT /precios-clasificacion?sedeId=8&granoId=4
        // Recibe la lista completa y hace upsert de cada fila
        [HttpPut]
        public async Task<IActionResult> GuardarClasificaciones(
            [FromBody] List<PrecioClasificacionDto> clasificaciones,
            [FromQuery] int sedeId,
            [FromQuery] int granoId)
        {
            if (clasificaciones == null || !clasificaciones.Any())
                return BadRequest("Lista vacía");

            if (_db.State == ConnectionState.Closed) _db.Open();
            using var trans = _db.BeginTransaction();

            try
            {
                foreach (var c in clasificaciones)
                {
                    var existe = await _db.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM dbo.precios_clasificacion WHERE id = @Id",
                        new { c.Id }, trans);

                    if (existe > 0)
                    {
                        await _db.ExecuteAsync(@"
                            UPDATE dbo.precios_clasificacion
                            SET precio_kg  = @PrecioKg,
                                activo     = @Activo,
                                updated_at = SYSDATETIMEOFFSET()
                            WHERE id = @Id",
                            new { c.PrecioKg, c.Activo, c.Id }, trans);
                    }
                    else
                    {
                        await _db.ExecuteAsync(@"
                            INSERT INTO dbo.precios_clasificacion
                                (sede_id, grano_id, nombre, codigo, precio_kg, activo, orden)
                            VALUES
                                (@SedeId, @GranoId, @Nombre, @Codigo, @PrecioKg, @Activo, @Orden)",
                            new
                            {
                                SedeId  = sedeId,
                                GranoId = granoId,
                                c.Nombre,
                                c.Codigo,
                                c.PrecioKg,
                                c.Activo,
                                c.Orden
                            }, trans);
                    }
                }

                trans.Commit();
                return Ok(new { message = "Precios guardados correctamente" });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, new { message = "Error al guardar precios", error = ex.Message });
            }
        }
    }
}
