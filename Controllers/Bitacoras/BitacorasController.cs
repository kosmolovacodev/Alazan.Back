using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("bitacoras")]
    public class BitacorasController : ControllerBase
    {
        private readonly IDbConnection _db;
        public BitacorasController(IDbConnection db) => _db = db;

        // ─── GET /bitacoras/stats?sedeId=X ───────────────────────────
        // Conteos por código de bitácora agrupados por status
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int sedeId)
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    codigo_bitacora   AS codigoBitacora,
                    seccion_codigo    AS seccionCodigo,
                    COUNT(*)          AS total,
                    SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente,
                    SUM(CASE WHEN status = 'Firmada'   THEN 1 ELSE 0 END) AS firmada,
                    SUM(CASE WHEN status = 'Impresa'   THEN 1 ELSE 0 END) AS impresa
                  FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  GROUP BY codigo_bitacora, seccion_codigo",
                new { SedeId = sedeId });
            return Ok(rows);
        }

        // ─── GET /bitacoras/{codigo}?sedeId=X ────────────────────────
        // Registros de una bitácora específica
        [HttpGet("{codigo}")]
        public async Task<IActionResult> GetRegistros(string codigo, [FromQuery] int sedeId)
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    id,
                    codigo_bitacora AS codigoBitacora,
                    seccion_codigo  AS seccionCodigo,
                    CONVERT(DATE, fecha) AS fecha,
                    status,
                    datos_json      AS datosJson,
                    sede_id         AS sedeId,
                    created_at      AS creadoEn
                  FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND codigo_bitacora = @Codigo
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  ORDER BY fecha DESC, id DESC",
                new { Codigo = codigo, SedeId = sedeId });

            // Deserialize datos_json for each row
            var result = rows.Select(r => new
            {
                r.id,
                r.codigoBitacora,
                r.seccionCodigo,
                r.fecha,
                r.status,
                datos = r.datosJson != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(r.datosJson)
                    : new Dictionary<string, object>(),
                r.sedeId,
                r.creadoEn,
            });

            return Ok(result);
        }

        // ─── POST /bitacoras/{codigo} ─────────────────────────────────
        [HttpPost("{codigo}")]
        public async Task<IActionResult> Crear(string codigo, [FromBody] BitacoraRegistroRequest dto)
        {
            try
            {
                var datosJson = JsonSerializer.Serialize(dto.Datos);
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.bitacoras_registros
                        (codigo_bitacora, seccion_codigo, fecha, status, datos_json, sede_id, activo, created_at)
                      VALUES
                        (@Codigo, @SeccionCodigo, @Fecha, 'Pendiente', @DatosJson, @SedeId, 1, GETDATE());
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        Codigo = codigo,
                        dto.SeccionCodigo,
                        Fecha = string.IsNullOrWhiteSpace(dto.Fecha)
                            ? DateTime.Today.ToString("yyyy-MM-dd")
                            : dto.Fecha,
                        DatosJson = datosJson,
                        dto.SedeId,
                    });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar registro", error = ex.Message });
            }
        }

        // ─── PUT /bitacoras/{id}/status ───────────────────────────────
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> ActualizarStatus(int id, [FromBody] StatusRequest dto)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.bitacoras_registros SET status = @Status WHERE id = @Id AND activo = 1",
                new { dto.Status, Id = id });
            return Ok();
        }

        // ─── DELETE /bitacoras/{id} ───────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.bitacoras_registros SET activo = 0 WHERE id = @Id",
                new { Id = id });
            return Ok();
        }
    }

    public class BitacoraRegistroRequest
    {
        public string Fecha         { get; set; } = "";
        public string SeccionCodigo { get; set; } = "";
        public int    SedeId        { get; set; }
        public Dictionary<string, string> Datos { get; set; } = new();
    }

    public class StatusRequest
    {
        public string Status { get; set; } = "Pendiente";
    }
}
