using Alazan.API.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Alazan.API.Controllers.Compras
{
    [Authorize]
    [ApiController]
    [Route("ordenes-compra")]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<OrdenesCompraController> _logger;
        private readonly SincronizacionOrdenesCompraService _syncService;

        public OrdenesCompraController(
            IDbConnection db,
            ILogger<OrdenesCompraController> logger,
            SincronizacionOrdenesCompraService syncService)
        {
            _db          = db;
            _logger      = logger;
            _syncService = syncService;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  POST /ordenes-compra/sincronizar
        //  Dispara la sincronización MBA3 manualmente (sin esperar la hora)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("sincronizar")]
        public async Task<IActionResult> Sincronizar()
        {
            try
            {
                _logger.LogInformation("Sincronización OC MBA3 disparada manualmente");
                await _syncService.SincronizarAsync();
                return Ok(new { message = "Sincronización completada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización manual OC MBA3");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /ordenes-compra
        //  Lista paginada. El estado_local se calcula en tiempo real.
        //  Params: page, pageSize, estadoLocal (-1=todos), productorId,
        //          fechaDesde, fechaHasta, search
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetOrdenesCompra(
            [FromQuery] int     page        = 1,
            [FromQuery] int     pageSize    = 50,
            [FromQuery] int     estadoLocal = -1,
            [FromQuery] long?   productorId = null,
            [FromQuery] string? fechaDesde  = null,
            [FromQuery] string? fechaHasta  = null,
            [FromQuery] string? search      = null)
        {
            Response.Headers["Cache-Control"] = "no-store";
            try
            {
                var offset = (page - 1) * pageSize;

                DateTime? desde = DateTime.TryParse(fechaDesde, out var fd) ? fd : null;
                DateTime? hasta = DateTime.TryParse(fechaHasta, out var fh) ? fh : null;

                // estado_local derivado en tiempo real cruzando con pagos locales
                const string estadoExpr = @"
                    CASE
                        WHEN oc.productor_id IS NULL THEN 0
                        WHEN EXISTS (
                            SELECT 1
                            FROM dbo.solicitudes_pago sp
                            INNER JOIN dbo.facturacion_recepciones fr ON fr.id = sp.facturacion_id
                            WHERE fr.productor_id = oc.productor_id
                              AND sp.status = 'PAGADO'
                        ) THEN 3
                        WHEN EXISTS (
                            SELECT 1
                            FROM dbo.solicitudes_pago sp
                            INNER JOIN dbo.facturacion_recepciones fr ON fr.id = sp.facturacion_id
                            WHERE fr.productor_id = oc.productor_id
                              AND sp.status IN ('SOLICITAR','PAGO SOLICITADO','AUTORIZADO')
                        ) THEN 2
                        ELSE 1
                    END";

                var where = new List<string>();
                if (productorId.HasValue)   where.Add("oc.productor_id = @productorId");
                if (desde.HasValue)         where.Add("oc.fecha_pedido >= @desde");
                if (hasta.HasValue)         where.Add("oc.fecha_pedido <= @hasta");
                if (!string.IsNullOrWhiteSpace(search))
                    where.Add("(oc.contrato_id_corp LIKE @search OR p.nombre LIKE @search OR oc.referencia_general LIKE @search)");

                var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

                // Si filtra por estado_local, envolvemos en CTE
                var havingEstado = estadoLocal >= 0
                    ? $"AND ({estadoExpr}) = @estadoLocal"
                    : "";

                var sql = $@"
                    SELECT COUNT(*) OVER () AS total,
                           oc.id,
                           oc.contrato_id_corp,
                           oc.contrato_id,
                           oc.fecha_pedido,
                           oc.fecha_entrega,
                           oc.inv_amount,
                           oc.currency_type,
                           oc.status         AS status_mba3,
                           oc.tipo_orden_compra,
                           oc.ware_code,
                           oc.pais_proveedor,
                           oc.ciudad_proveedor,
                           oc.referencia_general,
                           oc.productor_id,
                           p.nombre          AS nombre_productor,
                           oc.primera_sync,
                           oc.ultima_sync,
                           ({estadoExpr})    AS estado_local
                    FROM dbo.mba3_ordenes_compra oc
                    LEFT JOIN dbo.productores p ON p.id = oc.productor_id
                                               OR (oc.productor_id IS NULL
                                                   AND TRY_CAST(p.codigo_proveedor AS BIGINT) = TRY_CAST(oc.client_id AS BIGINT)
                                                   AND p.activo = 1)
                    {whereClause}
                    {(estadoLocal >= 0 ? $"HAVING ({estadoExpr}) = @estadoLocal" : "")}
                    ORDER BY oc.fecha_pedido DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                // HAVING no funciona sin GROUP BY en este contexto; usar subconsulta
                if (estadoLocal >= 0)
                {
                    sql = $@"
                        SELECT COUNT(*) OVER () AS total, sub.*
                        FROM (
                            SELECT oc.id,
                                   oc.contrato_id_corp,
                                   oc.contrato_id,
                                   oc.fecha_pedido,
                                   oc.fecha_entrega,
                                   oc.inv_amount,
                                   oc.currency_type,
                                   oc.status         AS status_mba3,
                                   oc.tipo_orden_compra,
                                   oc.ware_code,
                                   oc.pais_proveedor,
                                   oc.ciudad_proveedor,
                                   oc.referencia_general,
                                   oc.productor_id,
                                   p.nombre          AS nombre_productor,
                                   oc.primera_sync,
                                   oc.ultima_sync,
                                   ({estadoExpr})    AS estado_local
                            FROM dbo.mba3_ordenes_compra oc
                            LEFT JOIN dbo.productores p ON p.id = oc.productor_id
                                               OR (oc.productor_id IS NULL
                                                   AND TRY_CAST(p.codigo_proveedor AS BIGINT) = TRY_CAST(oc.client_id AS BIGINT)
                                                   AND p.activo = 1)
                            {whereClause}
                        ) sub
                        WHERE sub.estado_local = @estadoLocal
                        ORDER BY sub.fecha_pedido DESC
                        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";
                }

                var rows = await _db.QueryAsync<dynamic>(sql, new
                {
                    productorId,
                    desde,
                    hasta,
                    search = $"%{search}%",
                    estadoLocal,
                    offset,
                    pageSize
                });

                var list  = rows.AsList();
                var total = list.Count > 0 ? (int)(list[0].total ?? 0) : 0;

                var ultimaSync = await _db.ExecuteScalarAsync<DateTime?>(
                    "SELECT MAX(ultima_sync) FROM dbo.mba3_ordenes_compra");

                return Ok(new { total, items = list, ultima_sync = ultimaSync });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener órdenes de compra");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /ordenes-compra/{id}/historial
        //  Historial de cambios de STATUS para una OC
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}/historial")]
        public async Task<IActionResult> GetHistorial(int id)
        {
            try
            {
                var existe = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.mba3_ordenes_compra WHERE id = @id", new { id });

                if (existe == 0) return NotFound(new { message = "Orden de compra no encontrada" });

                var historial = await _db.QueryAsync<dynamic>(@"
                    SELECT id, orden_id, status_anterior, status_nuevo, fecha_cambio
                    FROM dbo.mba3_ordenes_compra_historial
                    WHERE orden_id = @id
                    ORDER BY fecha_cambio ASC", new { id });

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de OC {Id}", id);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
