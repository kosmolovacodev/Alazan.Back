using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("bodega")]
    public class BodegaController : ControllerBase
    {
        private readonly IDbConnection _db;
        public BodegaController(IDbConnection db) => _db = db;

        // ─── GET /bodega/catalogos?sedeId=1 ───────────────────────────────
        // Todos los catálogos que necesita la pantalla de asignación
        [HttpGet("catalogos")]
        public async Task<IActionResult> GetCatalogos([FromQuery] int sedeId)
        {
            var bodegas = await _db.QueryAsync(
                "SELECT id, clave, nombre FROM dbo.cat_bodega WHERE activo = 1 AND sede_id = @SedeId ORDER BY clave",
                new { SedeId = sedeId });

            var cuadrantes = await _db.QueryAsync(
                "SELECT id, clave, nombre FROM dbo.cat_cuadrante WHERE activo = 1 AND sede_id = @SedeId ORDER BY clave",
                new { SedeId = sedeId });

            // Reutiliza la tabla de presentaciones de producción
            var presentaciones = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_presentacion_produccion WHERE activo = 1 ORDER BY nombre");

            var tiposCostal = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_tipo_costal WHERE activo = 1 AND sede_id = @SedeId ORDER BY nombre",
                new { SedeId = sedeId });

            var subproductos = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_subproducto_bodega WHERE activo = 1 AND sede_id = @SedeId ORDER BY nombre",
                new { SedeId = sedeId });

            return Ok(new { bodegas, cuadrantes, presentaciones, tiposCostal, subproductos });
        }

        // ─── GET /bodega/historial?sedeId=1 ──────────────────────────────
        // Todas las órdenes de producción con su estado de bodega
        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial([FromQuery] int sedeId)
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    o.id                                                AS ordenId,
                    o.no_orden                                          AS folio,
                    CONVERT(DATE, o.fecha_orden)                        AS fecha,
                    o.status                                            AS statusProduccion,
                    o.calibre_tipo                                      AS calibre,
                    ISNULL(
                        (SELECT TOP 1 pt.producto
                         FROM dbo.produccion_trenes pt
                         WHERE pt.orden_id = o.id),
                        ''
                    )                                                   AS producto,
                    ISNULL(ba.id, 0)                                    AS asignacionId,
                    ba.fecha                                            AS fechaAsignacion,
                    ISNULL(ba.status_asignacion, 'No Asignado')         AS statusAsignacion,
                    ISNULL(
                        (SELECT SUM(bi.cantidad_total)
                         FROM dbo.bodega_asignacion_items bi
                         JOIN dbo.bodega_asignaciones ba2 ON ba2.id = bi.asignacion_id
                         WHERE ba2.orden_id = o.id AND bi.tipo = 'producto'),
                        0
                    )                                                   AS cantidadTons,
                    ISNULL(
                        (SELECT STRING_AGG(CAST(cb.clave AS NVARCHAR(MAX)) + '/' + CAST(cc.clave AS NVARCHAR(MAX)), ', ')
                         FROM dbo.bodega_asignacion_items bi2
                         JOIN dbo.bodega_asignaciones ba3 ON ba3.id = bi2.asignacion_id
                         JOIN dbo.bodega_asignacion_detalle bd ON bd.item_id = bi2.id
                         JOIN dbo.cat_bodega cb ON cb.id = bd.bodega_id
                         JOIN dbo.cat_cuadrante cc ON cc.id = bd.cuadrante_id
                         WHERE ba3.orden_id = o.id),
                        'Sin asignar'
                    )                                                   AS ubicacion
                  FROM dbo.ordenesproduccion o
                  LEFT JOIN dbo.bodega_asignaciones ba ON ba.orden_id = o.id
                  WHERE o.sede_id = @SedeId
                  ORDER BY o.fecha_orden DESC",
                new { SedeId = sedeId });

            return Ok(rows);
        }

        // ─── GET /bodega/stats?sedeId=1 ───────────────────────────────────
        // Toneladas por bodega (para las tarjetas B1-B6)
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int sedeId)
        {
            var porBodega = await _db.QueryAsync(
                @"SELECT
                    cb.id       AS bodegaId,
                    cb.clave    AS bodegaClave,
                    cb.nombre   AS bodegaNombre,
                    ISNULL(SUM(bd.cantidad), 0) AS totalTons
                  FROM dbo.cat_bodega cb
                  LEFT JOIN dbo.bodega_asignacion_detalle bd ON bd.bodega_id = cb.id
                  LEFT JOIN dbo.bodega_asignacion_items bi   ON bi.id = bd.item_id
                  LEFT JOIN dbo.bodega_asignaciones ba       ON ba.id = bi.asignacion_id
                  WHERE cb.sede_id = @SedeId AND cb.activo = 1
                    AND (ba.sede_id = @SedeId OR ba.sede_id IS NULL)
                  GROUP BY cb.id, cb.clave, cb.nombre
                  ORDER BY cb.clave",
                new { SedeId = sedeId });

            return Ok(new { porBodega });
        }

        // ─── GET /bodega/asignacion/{ordenId} ─────────────────────────────
        [HttpGet("asignacion/{ordenId}")]
        public async Task<IActionResult> GetAsignacion(int ordenId)
        {
            var asignacion = await _db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, orden_id AS ordenId, sede_id AS sedeId,
                         CONVERT(VARCHAR(10), fecha, 120) AS fecha,
                         status_asignacion AS statusAsignacion
                  FROM dbo.bodega_asignaciones
                  WHERE orden_id = @OrdenId",
                new { OrdenId = ordenId });

            if (asignacion == null)
                return Ok(null);

            var items = (await _db.QueryAsync(
                @"SELECT id, asignacion_id AS asignacionId, tipo, nombre,
                         presentacion_id AS presentacionId,
                         tipo_costal_id  AS tipoCostalId,
                         cantidad_total  AS cantidadTotal,
                         orden_item      AS ordenItem
                  FROM dbo.bodega_asignacion_items
                  WHERE asignacion_id = @AsignacionId
                  ORDER BY orden_item, id",
                new { AsignacionId = (int)asignacion.id })).ToList();

            var itemIds = items.Select(i => (int)i.id).ToList();
            IEnumerable<dynamic> detalle = Enumerable.Empty<dynamic>();

            if (itemIds.Count > 0)
            {
                detalle = await _db.QueryAsync(
                    $@"SELECT d.id, d.item_id AS itemId,
                              d.bodega_id AS bodegaId, d.cuadrante_id AS cuadranteId, d.cantidad,
                              cb.clave AS bodegaClave, cc.clave AS cuadranteClave
                       FROM dbo.bodega_asignacion_detalle d
                       JOIN dbo.cat_bodega    cb ON cb.id = d.bodega_id
                       JOIN dbo.cat_cuadrante cc ON cc.id = d.cuadrante_id
                       WHERE d.item_id IN ({string.Join(',', itemIds)})");
            }

            return Ok(new { asignacion, items, detalle });
        }

        // ─── POST /bodega/asignacion ───────────────────────────────────────
        [HttpPost("asignacion")]
        public async Task<IActionResult> GuardarAsignacion([FromBody] BodegaAsignacionRequest dto)
        {
            try
            {
                // Buscar asignación existente
                var asignacionId = await _db.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM dbo.bodega_asignaciones WHERE orden_id = @OrdenId",
                    new { dto.OrdenId });

                if (asignacionId == null)
                {
                    // Crear nueva
                    asignacionId = await _db.QuerySingleAsync<int>(
                        @"INSERT INTO dbo.bodega_asignaciones
                            (orden_id, sede_id, fecha, status_asignacion)
                          VALUES (@OrdenId, @SedeId, @Fecha, @Status);
                          SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new {
                            dto.OrdenId, dto.SedeId,
                            Fecha  = string.IsNullOrEmpty(dto.Fecha) ? (object)DBNull.Value : dto.Fecha,
                            Status = dto.StatusAsignacion ?? "Asignado"
                        });
                }
                else
                {
                    // Actualizar cabecera
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.bodega_asignaciones
                          SET fecha = @Fecha, status_asignacion = @Status,
                              fecha_actualizacion = SYSDATETIMEOFFSET()
                          WHERE id = @Id",
                        new {
                            Fecha  = string.IsNullOrEmpty(dto.Fecha) ? (object)DBNull.Value : dto.Fecha,
                            Status = dto.StatusAsignacion ?? "Asignado",
                            Id     = asignacionId
                        });

                    // Limpiar items y detalle existentes
                    await _db.ExecuteAsync(
                        @"DELETE d FROM dbo.bodega_asignacion_detalle d
                          JOIN dbo.bodega_asignacion_items i ON i.id = d.item_id
                          WHERE i.asignacion_id = @AsignacionId",
                        new { AsignacionId = asignacionId });

                    await _db.ExecuteAsync(
                        "DELETE FROM dbo.bodega_asignacion_items WHERE asignacion_id = @AsignacionId",
                        new { AsignacionId = asignacionId });
                }

                // Insertar items
                int orden = 0;
                foreach (var item in dto.Items)
                {
                    var itemId = await _db.QuerySingleAsync<int>(
                        @"INSERT INTO dbo.bodega_asignacion_items
                            (asignacion_id, tipo, nombre, presentacion_id, tipo_costal_id, cantidad_total, orden_item)
                          VALUES (@AsignacionId, @Tipo, @Nombre, @PresentacionId, @TipoCostalId, @CantidadTotal, @Orden);
                          SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new {
                            AsignacionId  = asignacionId,
                            item.Tipo,
                            item.Nombre,
                            PresentacionId = item.PresentacionId.HasValue ? (object)item.PresentacionId : DBNull.Value,
                            TipoCostalId   = item.TipoCostalId.HasValue   ? (object)item.TipoCostalId   : DBNull.Value,
                            CantidadTotal  = item.CantidadTotal,
                            Orden          = orden++
                        });

                    foreach (var det in item.Detalle.Where(d => d.BodegaId > 0 && d.CuadranteId > 0))
                    {
                        await _db.ExecuteAsync(
                            @"INSERT INTO dbo.bodega_asignacion_detalle (item_id, bodega_id, cuadrante_id, cantidad)
                              VALUES (@ItemId, @BodegaId, @CuadranteId, @Cantidad)",
                            new { ItemId = itemId, det.BodegaId, det.CuadranteId, det.Cantidad });
                    }
                }

                return Ok(new { message = "Asignación guardada", asignacionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar asignación", error = ex.Message });
            }
        }
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────
    public class BodegaAsignacionRequest
    {
        public int     OrdenId          { get; set; }
        public int     SedeId           { get; set; }
        public string? Fecha            { get; set; }
        public string? StatusAsignacion { get; set; }
        public List<BodegaItemRequest> Items { get; set; } = new();
    }

    public class BodegaItemRequest
    {
        public string  Tipo           { get; set; } = "producto";
        public string  Nombre         { get; set; } = "";
        public int?    PresentacionId { get; set; }
        public int?    TipoCostalId   { get; set; }
        public decimal CantidadTotal  { get; set; }
        public List<BodegaDetalleRequest> Detalle { get; set; } = new();
    }

    public class BodegaDetalleRequest
    {
        public int     BodegaId    { get; set; }
        public int     CuadranteId { get; set; }
        public decimal Cantidad    { get; set; }
    }
}
