using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Alazan.API.Controllers.Compras
{
    [Authorize]
    [ApiController]
    [Route("ordenes-compra-granos")]
    public class OrdenesCompraGranosController : ControllerBase
    {
        private readonly IDbConnection _db;

        public OrdenesCompraGranosController(IDbConnection db) => _db = db;

        // ═══════════════════════════════════════════════════════════════════
        //  GET /ordenes-compra-granos?sedeId=1&fecha=2026-05-07
        //  Lista de órdenes de compra generadas desde preliquidaciones.
        //  Por defecto muestra solo el día actual.
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetOrdenesCompra(
            [FromQuery] int     sedeId     = 0,
            [FromQuery] string? fecha      = null,
            [FromQuery] string? fechaDesde = null,
            [FromQuery] string? fechaHasta = null,
            [FromQuery] string? search     = null)
        {
            Response.Headers["Cache-Control"] = "no-store";
            try
            {
                // Si no se pasa fecha ni rango, usar hoy como predeterminado
                DateTime? diaFiltro = null;
                DateTime? desde     = null;
                DateTime? hasta     = null;

                if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out var fd))
                    diaFiltro = fd.Date;
                else if (!string.IsNullOrWhiteSpace(fechaDesde) || !string.IsNullOrWhiteSpace(fechaHasta))
                {
                    if (DateTime.TryParse(fechaDesde, out var fd2)) desde = fd2.Date;
                    if (DateTime.TryParse(fechaHasta, out var fh2)) hasta = fh2.Date.AddDays(1).AddTicks(-1);
                }
                else
                    diaFiltro = DateTime.Today;

                var where = new List<string>();
                where.Add("(@sedeId = 0 OR b.sede_id = @sedeId)");

                if (diaFiltro.HasValue)
                {
                    where.Add("CAST(p.created_at AS DATE) = @diaFiltro");
                }
                else
                {
                    if (desde.HasValue) where.Add("p.created_at >= @desde");
                    if (hasta.HasValue) where.Add("p.created_at <= @hasta");
                }

                if (!string.IsNullOrWhiteSpace(search))
                    where.Add("(b.folio LIKE @search OR b.ticket_numero LIKE @search OR b.productor LIKE @search)");

                var whereClause = "AND " + string.Join(" AND ", where);

                var sql = $@"
                    SELECT
                        oc.id                     AS ocId,
                        oc.folio                  AS folioOC,
                        oc.folio_mba3             AS folioMba3,
                        oc.status                 AS statusOC,
                        b.folio                   AS boleta,
                        b.ticket_numero           AS ticket,
                        b.productor               AS productor,
                        g.nombre                  AS producto,
                        p.peso_neto_kg            AS pesoNeto,
                        p.kg_a_liquidar           AS pesoALiquidar,
                        p.importe_total           AS importeAPagar,
                        FORMAT(p.created_at, 'yyyy-MM-dd') AS fecha,
                        p.id                      AS preliquidacionId
                    FROM dbo.ordenes_compra oc
                    INNER JOIN dbo.preliquidaciones p      ON p.id  = oc.preliquidacion_id
                    INNER JOIN dbo.boletas b               ON b.id  = p.boleta_id
                    LEFT  JOIN dbo.bascula_recepciones br  ON br.id = b.bascula_id
                    LEFT  JOIN dbo.granos_catalogo g       ON g.id  = br.grano_id
                    WHERE oc.activo = 1
                    {whereClause}
                    ORDER BY p.created_at DESC";

                var rows = await _db.QueryAsync<dynamic>(sql, new
                {
                    sedeId,
                    diaFiltro = diaFiltro?.Date,
                    desde,
                    hasta,
                    search = $"%{search}%"
                });

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /ordenes-compra-granos/{id}
        //  Detalle completo de una OC para mostrar el documento
        //  ORDEN DE COMPRA/LIQUIDACION
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetalle(int id, [FromQuery] int sedeId = 0)
        {
            try
            {
                var sql = @"
                    SELECT
                        oc.folio                      AS folioOC,
                        oc.folio_mba3                 AS folioMba3,
                        oc.status                     AS statusOC,
                        b.folio                       AS boleta,
                        b.ticket_numero               AS ticket,
                        b.productor                   AS productor,
                        b.comprador                   AS comprador,
                        b.origen                      AS origen,
                        b.precio_mxn                  AS precio,
                        g.nombre                      AS producto,
                        br.chofer                     AS chofer,
                        br.peso_bruto_kg              AS pesoBruto,
                        p.peso_neto_kg                AS pesoNeto,
                        p.kg_a_liquidar               AS pesoALiquidar,
                        p.importe_total               AS importeAPagar,
                        (p.peso_neto_kg - p.kg_a_liquidar) AS totalDeducciones,
                        s.nombre_sede                 AS bodegaRecepcion,
                        FORMAT(p.created_at, 'yyyy-MM-dd') AS fecha,
                        1                             AS diasPago
                    FROM dbo.preliquidaciones p
                    INNER JOIN dbo.ordenes_compra oc      ON oc.preliquidacion_id = p.id AND oc.activo = 1
                    INNER JOIN dbo.boletas b              ON b.id  = p.boleta_id
                    LEFT  JOIN dbo.bascula_recepciones br ON br.id = b.bascula_id
                    LEFT  JOIN dbo.granos_catalogo g      ON g.id  = br.grano_id
                    LEFT  JOIN dbo.sedes_catalogo s       ON s.id  = b.sede_id
                    WHERE p.id = @id
                      AND (@sedeId = 0 OR b.sede_id = @sedeId)";

                var detalle = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { id, sedeId });

                if (detalle == null)
                    return NotFound(new { message = "Orden de compra no encontrada" });

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PUT /ordenes-compra-granos/{ocId}/vincular-mba3
        //  Vincula una OC interna al folio MBA3 (contrato_id_corp)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPut("{ocId:int}/vincular-mba3")]
        public async Task<IActionResult> VincularMba3(int ocId, [FromBody] VincularMba3Dto dto)
        {
            try
            {
                var afectadas = await _db.ExecuteAsync(@"
                    UPDATE dbo.ordenes_compra
                    SET folio_mba3 = @folioMba3, updated_at = GETDATE()
                    WHERE id = @ocId AND activo = 1",
                    new { ocId, dto.FolioMba3 });

                if (afectadas == 0)
                    return NotFound(new { message = "Orden de compra no encontrada" });

                return Ok(new { message = "Folio MBA3 vinculado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class VincularMba3Dto
    {
        public string? FolioMba3 { get; set; }
    }
}
