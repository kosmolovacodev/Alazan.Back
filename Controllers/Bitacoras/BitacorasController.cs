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

        // ─── GET /bitacoras/secciones?sedeId=X ───────────────────────
        // Secciones activas con conteo de bitácoras y pendientes
        [HttpGet("secciones")]
        public async Task<IActionResult> GetSecciones([FromQuery] int sedeId)
        {
            // Estadísticas de pendientes por sección (solo bitácoras manuales en bitacoras_registros)
            var stats = await _db.QueryAsync(
                @"SELECT seccion_codigo AS seccionCodigo,
                         SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente
                   FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  GROUP BY seccion_codigo",
                new { SedeId = sedeId });

            var statsBySec = stats.ToDictionary(
                r => (string)r.seccionCodigo,
                r => (int)r.pendiente);

            var secciones = await _db.QueryAsync(
                @"SELECT
                    s.codigo,
                    s.nombre,
                    s.descripcion,
                    s.icono,
                    s.color,
                    s.orden,
                    COUNT(d.codigo) AS totalBitacoras
                  FROM dbo.bitacoras_secciones s
                  LEFT JOIN dbo.bitacoras_definicion d
                    ON d.seccion_codigo = s.codigo AND d.activo = 1
                  WHERE s.activo = 1
                  GROUP BY s.codigo, s.nombre, s.descripcion, s.icono, s.color, s.orden
                  ORDER BY s.orden");

            var result = secciones.Select(s => new
            {
                s.codigo,
                s.nombre,
                descripcion = (string?)s.descripcion,
                s.icono,
                s.color,
                s.orden,
                totalBitacoras = (int)s.totalBitacoras,
                pendiente = statsBySec.TryGetValue((string)s.codigo, out var p) ? p : 0,
            });

            return Ok(result);
        }

        // ─── GET /bitacoras/definicion/{seccion}?sedeId=X ────────────
        // Bitácoras de una sección con su conteo de pendientes
        [HttpGet("definicion/{seccion}")]
        public async Task<IActionResult> GetDefinicion(string seccion, [FromQuery] int sedeId)
        {
            var bitacoras = await _db.QueryAsync(
                @"SELECT
                    d.codigo,
                    d.nombre,
                    d.tipo,
                    d.fuente_query AS fuenteQuery,
                    d.orden,
                    ISNULL(stats.pendiente, 0) AS pendiente
                  FROM dbo.bitacoras_definicion d
                  LEFT JOIN (
                      SELECT codigo_bitacora,
                             SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente
                        FROM dbo.bitacoras_registros
                       WHERE activo = 1
                         AND (@SedeId = 0 OR sede_id = @SedeId)
                       GROUP BY codigo_bitacora
                  ) stats ON stats.codigo_bitacora = d.codigo
                  WHERE d.seccion_codigo = @Seccion
                    AND d.activo = 1
                  ORDER BY d.orden",
                new { Seccion = seccion, SedeId = sedeId });

            return Ok(bitacoras);
        }

        // ─── GET /bitacoras/columnas/{codigo} ────────────────────────
        // Definición de columnas de una bitácora
        [HttpGet("columnas/{codigo}")]
        public async Task<IActionResult> GetColumnas(string codigo)
        {
            var columnas = await _db.QueryAsync(
                @"SELECT
                    campo,
                    label,
                    tipo_dato AS tipoDato,
                    es_meta   AS esMeta,
                    orden
                  FROM dbo.bitacoras_columnas
                  WHERE codigo_bitacora = @Codigo
                    AND visible = 1
                  ORDER BY orden",
                new { Codigo = codigo });

            return Ok(columnas);
        }

        // ─── GET /bitacoras/stats?sedeId=X ───────────────────────────
        // Conteos por código de bitácora agrupados por status (solo manuales)
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
        // Registros de una bitácora: ruteado a vista o a tabla según tipo
        [HttpGet("{codigo}")]
        public async Task<IActionResult> GetRegistros(string codigo, [FromQuery] int sedeId)
        {
            // Leer definición para saber si es linked o manual
            var def = await _db.QueryFirstOrDefaultAsync(
                @"SELECT tipo, fuente_query AS fuenteQuery
                    FROM dbo.bitacoras_definicion
                   WHERE codigo = @Codigo AND activo = 1",
                new { Codigo = codigo });

            if (def == null)
                return NotFound(new { message = $"Bitácora '{codigo}' no encontrada" });

            string tipo = (string)def.tipo;
            string? fuenteQuery = (string?)def.fuenteQuery;

            // ── LINKED: datos vienen de vista operacional ─────────────
            if (tipo == "linked" && !string.IsNullOrWhiteSpace(fuenteQuery))
            {
                // Validar nombre de vista para prevenir SQL injection
                // Solo se aceptan nombres que empiecen con vw_bitacora_
                if (!fuenteQuery.StartsWith("vw_bitacora_", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Nombre de vista inválido" });

                var sql = $@"SELECT * FROM dbo.{fuenteQuery}
                             WHERE (@SedeId = 0 OR sede_id = @SedeId)
                             ORDER BY fecha DESC, id DESC";

                var rows = await _db.QueryAsync(sql, new { SedeId = sedeId });

                // Serializar a formato compatible con el frontend
                var result = rows.Select(r =>
                {
                    var dict = (IDictionary<string, object?>)r;
                    var datos = new Dictionary<string, object?>();
                    foreach (var kv in dict)
                    {
                        var key = kv.Key.ToLowerInvariant();
                        if (key is "id" or "sede_id" or "fecha" or "status") continue;
                        datos[key] = kv.Value;
                    }
                    return new
                    {
                        id       = dict.TryGetValue("id", out var id) ? id : null,
                        codigoBitacora = codigo,
                        fecha    = dict.TryGetValue("fecha", out var f) ? f : null,
                        status   = dict.TryGetValue("status", out var s) ? s : "Pendiente",
                        datos,
                        sedeId,
                    };
                });

                return Ok(result);
            }

            // ── MANUAL: datos vienen de bitacoras_registros ───────────
            var manualRows = await _db.QueryAsync(
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

            var manualResult = manualRows.Select(r => new
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

            return Ok(manualResult);
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
