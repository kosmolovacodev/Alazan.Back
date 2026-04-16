using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("produccion")]
    public class ProduccionController : ControllerBase
    {
        private readonly IDbConnection _db;
        public ProduccionController(IDbConnection db) => _db = db;

        // ─── CATÁLOGOS para el módulo ───────────────────────────────────
        [HttpGet("catalogos")]
        public async Task<IActionResult> GetCatalogos([FromQuery] int sedeId)
        {
            var trenes = await _db.QueryAsync(
                "SELECT id, nombre, sede_id AS sedeId FROM dbo.cat_trenes_produccion WHERE activo = 1 AND sede_id = @sedeId ORDER BY nombre",
                new { sedeId });

            var tipoProceso = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_tipoproceso_produccion WHERE activo = 1 ORDER BY nombre");

            var presentacion = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_presentacion_produccion WHERE activo = 1 ORDER BY nombre");

            var bloqueInsumos = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_bloqueinsumos_produccion WHERE activo = 1 ORDER BY nombre");

            var subproductos = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_subproducto_produccion WHERE activo = 1 ORDER BY nombre");

            var desechos = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_desecho_produccion WHERE activo = 1 ORDER BY nombre");

            var silos = await _db.QueryAsync(
                @"SELECT id, nombre FROM dbo.silos_calibre_catalogo
                  WHERE activo = 1 AND sede_id = @sedeId
                  ORDER BY CAST(nombre AS NVARCHAR(4000))",
                new { sedeId });

            var bodegas = await _db.QueryAsync(
                "SELECT id, CAST(nombre_almacen AS NVARCHAR(4000)) AS nombre FROM dbo.catalogo_almacenes WHERE sede_id = @sedeId AND activo = 1 ORDER BY CAST(nombre_almacen AS NVARCHAR(4000))",
                new { sedeId });

            var calibres = await _db.QueryAsync(
                "SELECT id, calibre_mm, calibre_mm AS nombre, grano_id AS granoId FROM dbo.calibres_catalogo WHERE activo = 1 AND sede_id = @sedeId ORDER BY calibre_mm",
                new { sedeId });

            // Calibres por tipo de mercado para el selector del encabezado de orden
            var calibreOzAm = await _db.QueryAsync(
                @"SELECT c.id, c.calibre_mm AS nombre FROM dbo.calibres_catalogo c
                  JOIN dbo.granos_catalogo g ON g.id = c.grano_id
                  WHERE g.nombre = 'Garbanzo' AND c.clasificacion = 'OZ AM'
                    AND c.activo = 1 AND c.sede_id = @sedeId ORDER BY c.id",
                new { sedeId });

            var calibreOzEsp = await _db.QueryAsync(
                @"SELECT c.id, c.calibre_mm AS nombre FROM dbo.calibres_catalogo c
                  JOIN dbo.granos_catalogo g ON g.id = c.grano_id
                  WHERE g.nombre = 'Garbanzo' AND c.clasificacion = 'OZ ESP'
                    AND c.activo = 1 AND c.sede_id = @sedeId ORDER BY c.id",
                new { sedeId });

            var calibreFrijol = await _db.QueryAsync(
                @"SELECT c.id, c.calibre_mm AS nombre FROM dbo.calibres_catalogo c
                  JOIN dbo.granos_catalogo g ON g.id = c.grano_id
                  WHERE g.nombre = 'Frijol'
                    AND c.activo = 1 AND c.sede_id = @sedeId ORDER BY c.id",
                new { sedeId });

            var calibresAnalisis = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_calibres_analisis_produccion WHERE activo = 1 ORDER BY CASE WHEN nombre LIKE '%-%' THEN 1 ELSE 0 END, LEN(nombre), nombre");

            return Ok(new { trenes, tipoProceso, presentacion, bloqueInsumos, subproductos, desechos, silos, bodegas, calibres, calibreOzAm, calibreOzEsp, calibreFrijol, calibresAnalisis });
        }

        // ─── ÓRDENES DE PRODUCCIÓN ──────────────────────────────────────
        [HttpGet("ordenes")]
        public async Task<IActionResult> GetOrdenes([FromQuery] int sedeId)
        {
            var ordenes = await _db.QueryAsync(
                @"SELECT
                    o.id,
                    o.no_orden        AS noOrden,
                    o.fecha_orden     AS fechaOrden,
                    o.status,
                    o.calibre_tipo    AS calibreTipo,
                    o.fecha_creacion  AS fechaCreacion,
                    -- Número de trenes
                    (SELECT COUNT(*) FROM dbo.produccion_trenes t WHERE t.orden_id = o.id) AS numTrenes,
                    -- KG total del resultado
                    ISNULL((
                        SELECT SUM(
                            ISNULL(JSON_VALUE(tr.value, '$.sacos25')  * 25, 0) +
                            ISNULL(JSON_VALUE(tr.value, '$.sacos50')  * 50, 0) +
                            ISNULL(CAST(JSON_VALUE(tr.value, '$.polibolsa') AS DECIMAL(12,2)), 0)
                        )
                        FROM dbo.resultado_produccion rp
                        CROSS APPLY OPENJSON(rp.producto_clasificado) tr
                        WHERE rp.orden_id = o.id
                    ), 0) AS kg,
                    -- Producto (primer tren)
                    (SELECT TOP 1 t.producto FROM dbo.produccion_trenes t WHERE t.orden_id = o.id) AS producto
                FROM dbo.ordenesproduccion o
                WHERE o.sede_id = @sedeId
                ORDER BY o.fecha_creacion DESC",
                new { sedeId });

            return Ok(ordenes);
        }

        [HttpGet("ordenes/{id}")]
        public async Task<IActionResult> GetOrden(int id)
        {
            var orden = await _db.QueryFirstOrDefaultAsync(
                @"SELECT id, no_orden AS noOrden, sede_id AS sedeId, fecha_orden AS fechaOrden,
                         calibre_tipo AS calibreTipo,
                         status, justificacion_edicion AS justificacionEdicion,
                         fecha_creacion AS fechaCreacion
                  FROM dbo.ordenesproduccion WHERE id = @id",
                new { id });

            if (orden == null) return NotFound();

            var trenes = await _db.QueryAsync(
                @"SELECT
                    pt.id, pt.orden_id AS ordenId, pt.tren_id AS trenId,
                    ctp.nombre AS trenNombre,
                    pt.fecha, pt.maniobra,
                    pt.tipo_proceso_id AS tipoprocesoId,
                    tproc.nombre AS tipoprocesoNombre,
                    pt.presentacion_id AS presentacionId,
                    pres.nombre AS presentacionNombre,
                    pt.producto, pt.grano_id AS granoId,
                    pt.origen, pt.total_mp_suministrada AS totalMpSuministrada,
                    pt.bloque_insumos AS bloqueInsumos
                  FROM dbo.produccion_trenes pt
                  LEFT JOIN dbo.cat_trenes_produccion ctp ON pt.tren_id = ctp.id
                  LEFT JOIN dbo.cat_tipoproceso_produccion tproc ON pt.tipo_proceso_id = tproc.id
                  LEFT JOIN dbo.cat_presentacion_produccion pres ON pt.presentacion_id = pres.id
                  WHERE pt.orden_id = @id",
                new { id });

            var resultado = await _db.QueryFirstOrDefaultAsync(
                @"SELECT id, orden_id AS ordenId,
                         fecha_inicio AS fechaInicio, hora_inicio AS horaInicio,
                         fecha_fin AS fechaFin, hora_fin AS horaFin,
                         producto_clasificado AS productoClasificado,
                         subproducto, desecho, fecha_registro AS fechaRegistro
                  FROM dbo.resultado_produccion WHERE orden_id = @id",
                new { id });

            return Ok(new { orden, trenes, resultado });
        }

        [HttpPost("ordenes")]
        public async Task<IActionResult> CrearOrden([FromBody] OrdenRequest dto, [FromQuery] int sedeId)
        {
            try
            {
                if (_db.State == ConnectionState.Closed) _db.Open();
                using var trans = _db.BeginTransaction();

                var ordenId = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.ordenesproduccion (no_orden, sede_id, fecha_orden, calibre_tipo)
                      VALUES (@NoOrden, @SedeId, @FechaOrden, @CalibreTipo);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { NoOrden = dto.NoOrden, SedeId = sedeId, FechaOrden = dto.FechaOrden, CalibreTipo = dto.CalibreTipo },
                    transaction: trans);

                foreach (var tren in dto.Trenes)
                {
                    await _db.ExecuteAsync(
                        @"INSERT INTO dbo.produccion_trenes
                            (orden_id, tren_id, fecha, maniobra, tipo_proceso_id, presentacion_id,
                             producto, grano_id, origen, total_mp_suministrada, bloque_insumos)
                          VALUES
                            (@OrdenId, @TrenId, @Fecha, @Maniobra, @TipoprocesoId, @PresentacionId,
                             @Producto, @GranoId, @Origen, @TotalMp, @BloqueInsumos)",
                        new
                        {
                            OrdenId = ordenId,
                            TrenId = tren.TrenId,
                            Fecha = tren.Fecha,
                            Maniobra = tren.Maniobra,
                            TipoprocesoId = tren.TipoprocesoId,
                            PresentacionId = tren.PresentacionId,
                            Producto = tren.Producto,
                            GranoId = tren.GranoId,
                            Origen = tren.Origen,
                            TotalMp = tren.TotalMpSuministrada,
                            BloqueInsumos = tren.BloqueInsumos
                        },
                        transaction: trans);
                }

                trans.Commit();
                return Ok(new { id = ordenId, message = "Orden creada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear orden", error = ex.Message });
            }
        }

        [HttpPut("ordenes/{id}")]
        public async Task<IActionResult> EditarOrden(int id, [FromBody] OrdenRequest dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.JustificacionEdicion))
                    return BadRequest(new { message = "La justificación de edición es obligatoria" });

                if (_db.State == ConnectionState.Closed) _db.Open();
                using var trans = _db.BeginTransaction();

                await _db.ExecuteAsync(
                    @"UPDATE dbo.ordenesproduccion
                      SET no_orden = @NoOrden, fecha_orden = @FechaOrden,
                          calibre_tipo = @CalibreTipo,
                          justificacion_edicion = @Justificacion,
                          fecha_actualizacion = SYSDATETIMEOFFSET()
                      WHERE id = @Id",
                    new { NoOrden = dto.NoOrden, FechaOrden = dto.FechaOrden, CalibreTipo = dto.CalibreTipo, Justificacion = dto.JustificacionEdicion, Id = id },
                    transaction: trans);

                await _db.ExecuteAsync("DELETE FROM dbo.produccion_trenes WHERE orden_id = @Id", new { Id = id }, transaction: trans);

                foreach (var tren in dto.Trenes)
                {
                    await _db.ExecuteAsync(
                        @"INSERT INTO dbo.produccion_trenes
                            (orden_id, tren_id, fecha, maniobra, tipo_proceso_id, presentacion_id,
                             producto, grano_id, origen, total_mp_suministrada, bloque_insumos)
                          VALUES
                            (@OrdenId, @TrenId, @Fecha, @Maniobra, @TipoprocesoId, @PresentacionId,
                             @Producto, @GranoId, @Origen, @TotalMp, @BloqueInsumos)",
                        new
                        {
                            OrdenId = id,
                            TrenId = tren.TrenId,
                            Fecha = tren.Fecha,
                            Maniobra = tren.Maniobra,
                            TipoprocesoId = tren.TipoprocesoId,
                            PresentacionId = tren.PresentacionId,
                            Producto = tren.Producto,
                            GranoId = tren.GranoId,
                            Origen = tren.Origen,
                            TotalMp = tren.TotalMpSuministrada,
                            BloqueInsumos = tren.BloqueInsumos
                        },
                        transaction: trans);
                }

                trans.Commit();
                return Ok(new { message = "Orden actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al editar orden", error = ex.Message });
            }
        }

        // ─── RESULTADO DE PRODUCCIÓN ────────────────────────────────────
        [HttpGet("resultado/{ordenId}")]
        public async Task<IActionResult> GetResultado(int ordenId)
        {
            var resultado = await _db.QueryFirstOrDefaultAsync(
                @"SELECT id, orden_id AS ordenId,
                         fecha_inicio AS fechaInicio, hora_inicio AS horaInicio,
                         fecha_fin AS fechaFin, hora_fin AS horaFin,
                         producto_clasificado AS productoClasificado,
                         subproducto, desecho, fecha_registro AS fechaRegistro
                  FROM dbo.resultado_produccion WHERE orden_id = @ordenId",
                new { ordenId });

            return Ok(resultado);
        }

        [HttpPost("resultado")]
        public async Task<IActionResult> GuardarResultado([FromBody] ResultadoRequest dto)
        {
            try
            {
                var existe = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.resultado_produccion WHERE orden_id = @OrdenId",
                    new { OrdenId = dto.OrdenId });

                if (existe > 0)
                {
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.resultado_produccion
                          SET fecha_inicio = @FechaInicio, hora_inicio = @HoraInicio,
                              fecha_fin = @FechaFin, hora_fin = @HoraFin,
                              producto_clasificado = @ProductoClasificado,
                              subproducto = @Subproducto, desecho = @Desecho
                          WHERE orden_id = @OrdenId",
                        new
                        {
                            dto.OrdenId, dto.FechaInicio, dto.HoraInicio, dto.FechaFin, dto.HoraFin,
                            ProductoClasificado = dto.ProductoClasificado,
                            dto.Subproducto, dto.Desecho
                        });
                }
                else
                {
                    await _db.ExecuteAsync(
                        @"INSERT INTO dbo.resultado_produccion
                            (orden_id, fecha_inicio, hora_inicio, fecha_fin, hora_fin,
                             producto_clasificado, subproducto, desecho)
                          VALUES
                            (@OrdenId, @FechaInicio, @HoraInicio, @FechaFin, @HoraFin,
                             @ProductoClasificado, @Subproducto, @Desecho)",
                        new
                        {
                            dto.OrdenId, dto.FechaInicio, dto.HoraInicio, dto.FechaFin, dto.HoraFin,
                            ProductoClasificado = dto.ProductoClasificado,
                            dto.Subproducto, dto.Desecho
                        });
                }

                await _db.ExecuteAsync(
                    "UPDATE dbo.ordenesproduccion SET status = 'Resultado Registrado', fecha_actualizacion = SYSDATETIMEOFFSET() WHERE id = @Id",
                    new { Id = dto.OrdenId });

                return Ok(new { message = "Resultado guardado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar resultado", error = ex.Message });
            }
        }

        // ─── ANÁLISIS DE CALIDAD ────────────────────────────────────────
        [HttpGet("analisis")]
        public async Task<IActionResult> GetAnalisis([FromQuery] int sedeId)
        {
            var lista = await _db.QueryAsync(
                @"SELECT acp.id, acp.no_orden AS noOrden, acp.fecha, acp.envasado, acp.producto,
                         acp.cosecha, acp.proceso, acp.silo, acp.variedad,
                         ISNULL(acp.finalizado, 0) AS finalizado,
                         acp.fecha_registro AS fechaRegistro
                  FROM dbo.analisiscalidad_proceso acp
                  WHERE acp.sede_id = @sedeId
                  ORDER BY acp.fecha_registro DESC",
                new { sedeId });
            return Ok(lista);
        }

        [HttpPost("analisis/{id}/finalizar")]
        public async Task<IActionResult> FinalizarAnalisis(int id)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"UPDATE dbo.analisiscalidad_proceso
                      SET finalizado = 1, fecha_actualizacion = SYSDATETIMEOFFSET()
                      WHERE id = @Id",
                    new { Id = id });
                return Ok(new { message = "Análisis finalizado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al finalizar análisis", error = ex.Message });
            }
        }

        [HttpGet("analisis/{id}")]
        public async Task<IActionResult> GetAnalisisDetalle(int id)
        {
            var row = await _db.QueryFirstOrDefaultAsync(
                @"SELECT id, sede_id AS sedeId, no_orden AS noOrden, fecha, envasado, producto,
                         grano_id AS granoId, cosecha, proceso, silo,
                         ISNULL(finalizado, 0) AS finalizado,
                         detallado, parrillas, fecha_registro AS fechaRegistro
                  FROM dbo.analisiscalidad_proceso WHERE id = @id",
                new { id });
            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost("analisis")]
        public async Task<IActionResult> CrearAnalisis([FromBody] AnalisisRequest dto, [FromQuery] int sedeId)
        {
            try
            {
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.analisiscalidad_proceso
                        (sede_id, no_orden, fecha, envasado, producto, grano_id, cosecha, proceso, silo, variedad, detallado, parrillas)
                      VALUES
                        (@SedeId, @NoOrden, @Fecha, @Envasado, @Producto, @GranoId, @Cosecha, @Proceso, @Silo, @Variedad, @Detallado, @Parrillas);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        SedeId = sedeId, dto.NoOrden, dto.Fecha, dto.Envasado, dto.Producto,
                        dto.GranoId, dto.Cosecha, dto.Proceso, dto.Silo, dto.Variedad,
                        dto.Detallado, dto.Parrillas
                    });

                return Ok(new { id, message = "Análisis creado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear análisis", error = ex.Message });
            }
        }

        [HttpPut("analisis/{id}")]
        public async Task<IActionResult> ActualizarAnalisis(int id, [FromBody] AnalisisRequest dto)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"UPDATE dbo.analisiscalidad_proceso
                      SET detallado = @Detallado, parrillas = @Parrillas,
                          finalizado = 1,
                          fecha_actualizacion = SYSDATETIMEOFFSET()
                      WHERE id = @Id",
                    new { dto.Detallado, dto.Parrillas, Id = id });

                return Ok(new { message = "Análisis actualizado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar análisis", error = ex.Message });
            }
        }

        // ─── DTOs ───────────────────────────────────────────────────────
        public class OrdenRequest
        {
            public string NoOrden { get; set; } = "";
            public string FechaOrden { get; set; } = "";
            public string? CalibreTipo { get; set; }
            public string? JustificacionEdicion { get; set; }
            public List<TrenRequest> Trenes { get; set; } = new();
        }

        public class TrenRequest
        {
            public int TrenId { get; set; }
            public string Fecha { get; set; } = "";
            public string? Maniobra { get; set; }
            public int? TipoprocesoId { get; set; }
            public int? PresentacionId { get; set; }
            public string? Producto { get; set; }
            public int? GranoId { get; set; }
            public string? Origen { get; set; }
            public decimal? TotalMpSuministrada { get; set; }
            public string? BloqueInsumos { get; set; }
        }

        public class ResultadoRequest
        {
            public int OrdenId { get; set; }
            public string? FechaInicio { get; set; }
            public string? HoraInicio { get; set; }
            public string? FechaFin { get; set; }
            public string? HoraFin { get; set; }
            public string? ProductoClasificado { get; set; }
            public string? Subproducto { get; set; }
            public string? Desecho { get; set; }
        }

        public class AnalisisRequest
        {
            public string? NoOrden { get; set; }
            public string Fecha { get; set; } = "";
            public string? Envasado { get; set; }
            public string? Producto { get; set; }
            public int? GranoId { get; set; }
            public string? Cosecha { get; set; }
            public string? Proceso { get; set; }
            public string? Silo { get; set; }
            public string? Variedad { get; set; }
            public string? Detallado { get; set; }
            public string? Parrillas { get; set; }
        }
    }
}
