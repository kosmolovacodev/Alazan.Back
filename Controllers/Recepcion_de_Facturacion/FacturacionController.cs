using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using SistemaAlazan.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Alazan.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FacturacionController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<FacturacionController> _logger;

        public FacturacionController(IDbConnection db, ILogger<FacturacionController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════
        //  GET /facturacion/configuracion-completa?sedeId=8
        // ═══════════════════════════════════════════════
        [HttpGet("configuracion-completa")]
        public async Task<IActionResult> GetConfiguracionCompleta([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT * FROM Configuracion_Facturacion_General WHERE sede_id = @sedeId;
                    SELECT * FROM Configuracion_Facturacion_Status WHERE sede_id = @sedeId AND activo = 1 ORDER BY orden;
                    SELECT * FROM Configuracion_Facturacion_Documentos WHERE sede_id = @sedeId AND activo = 1;
                    SELECT * FROM Configuracion_Facturacion_Endoso WHERE sede_id = @sedeId;";

                using var multi = await _db.QueryMultipleAsync(sql, new { sedeId });

                var response = new
                {
                    General = await multi.ReadFirstOrDefaultAsync<ConfigFacturacionGeneral>(),
                    Estados = await multi.ReadAsync<FacturacionStatus>(),
                    DocumentosRequeridos = await multi.ReadAsync<FacturacionDocumentoConfig>(),
                    EndosoConfig = await multi.ReadFirstOrDefaultAsync<dynamic>()
                };

                if (response.General == null)
                    return NotFound(new { message = $"No hay configuración para la sede {sedeId}" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  GET /facturacion/recepciones?sedeId=8
        //  Alimenta la tabla principal de FacturacionPage.vue
        //
        //  JOINs:
        //    fr  → facturacion_recepciones
        //    pl  → preliquidaciones
        //    b   → boletas
        //    br  → bascula_recepciones
        //    p   → productores
        //    bp  → boletas_precio (fallback para registros viejos)
        //    ac  → analisis_calidad (humedad/impurezas para detalle)
        // ═══════════════════════════════════════════════
        [HttpGet("recepciones")]
        public async Task<IActionResult> GetRecepciones([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        ROW_NUMBER() OVER (ORDER BY fr.created_at DESC)     AS id,
                        ISNULL(b.ticket_numero, CAST(pl.ticket_numero AS VARCHAR)) AS ticket,
                        FORMAT(ISNULL(fr.fecha_recepcion, fr.created_at), 'dd/MM/yyyy') AS fecha,

                        -- RFC: fr.rfc_productor → productores.rfc → 'Pendiente RFC'
                        ISNULL(
                            NULLIF(fr.rfc_productor, ''),
                            ISNULL(NULLIF(p.rfc, ''), 'Pendiente RFC')
                        ) AS rfc,

                        -- Productor: productores.nombre → boletas_precio.productor_nombre
                        ISNULL(
                            CASE
                                WHEN p.tipo_persona = 'Moral' THEN ISNULL(p.atiende, p.nombre)
                                ELSE p.nombre
                            END,
                            bp.productor_nombre
                        ) AS productor,

                        -- Toneladas: fr.kg_total → pl.kg_a_liquidar → pl.peso_neto_kg (/ 1000)
                        CAST(
                            ISNULL(fr.kg_total_entregados,
                                ISNULL(pl.kg_a_liquidar,
                                    ISNULL(pl.peso_neto_kg, 0)
                                )
                            ) / 1000.0
                        AS DECIMAL(10,3)) AS toneladas,

                        -- Neto a pagar KG
                        ISNULL(fr.kg_total_entregados, ISNULL(pl.kg_a_liquidar, 0)) AS netoAPagar,

                        -- Precio: fr.precio_promedio → pl.precio_base_mxn_ton → bp.precio_final
                        ISNULL(fr.precio_promedio,
                            ISNULL(pl.precio_base_mxn_ton,
                                ISNULL(bp.precio_final, 0)
                            )
                        ) AS precio,

                        -- Importe
                        ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) AS importe,

                        -- A pagar (importe * 0.987 retención federal)
                        CAST(
                            ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) * 0.987
                        AS DECIMAL(18,2)) AS aPagar,

                        fr.status,
                        ISNULL(fr.tiene_documentos, 0)  AS tieneDocumentos,
                        ISNULL(fr.tiene_factura_xml, 0)  AS tieneFacturaXML,
                        fr.sede_id AS sedeId

                    FROM dbo.facturacion_recepciones fr
                    LEFT JOIN dbo.preliquidaciones    pl ON fr.preliquidacion_id = pl.id
                    LEFT JOIN dbo.boletas             b  ON fr.boleta_id = b.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.productores         p  ON ISNULL(br.productor_id, fr.productor_id) = p.id
                    LEFT JOIN dbo.boletas_precio      bp ON bp.boleta_id = b.id
                    WHERE (@sedeId = 0 OR fr.sede_id = @sedeId)
                    ORDER BY fr.created_at DESC";

                var recepciones = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(recepciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener recepciones", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  POST /facturacion/guardar-recepcion
        //  INSERT completo con todas las columnas de la tabla
        // ═══════════════════════════════════════════════
        [HttpPost("guardar-recepcion")]
        public async Task<IActionResult> GuardarRecepcion([FromBody] FacturacionRecepcion dto)
        {
            try
            {
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(usuarioIdClaim, out var uid))
                    dto.UsuarioRegistroId = uid;

                var sql = @"
                    INSERT INTO facturacion_recepciones
                    (productor_id, uuid_fiscal, serie, folio, monto_total, status,
                     preliquidacion_id, boleta_id, entrega_agrupada_id, fecha_recepcion,
                     rfc_productor, xml_factura_path, importe_factura, kg_total_entregados,
                     precio_promedio, tiene_documentos, tiene_factura_xml, usuario_registro_id,
                     created_at, updated_at, sede_id)
                    VALUES
                    (@ProductorId, @UuidFiscal, @Serie, @Folio, @MontoTotal, @Status,
                     @PreliquidacionId, @BoletaId, @EntregaAgrupadaId, @FechaRecepcion,
                     @RfcProductor, @XmlFacturaPath, @ImporteFactura, @KgTotalEntregados,
                     @PrecioPromedio, @TieneDocumentos, @TieneFacturaXml, @UsuarioRegistroId,
                     GETDATE(), GETDATE(), @SedeId);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var id = await _db.QuerySingleAsync<int>(sql, dto);
                return Ok(new { success = true, id_generado = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar en DB", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  PUT /facturacion/actualizar-rfc
        //  Actualiza rfc_productor en facturacion_recepciones
        //  y el rfc en la tabla productores (fuente maestra)
        // ═══════════════════════════════════════════════
        [HttpPut("actualizar-rfc")]
        public async Task<IActionResult> ActualizarRfc([FromBody] ActualizarRfcRequest dto)
        {
            try
            {
                if (dto.Tickets.Length == 0 || string.IsNullOrWhiteSpace(dto.NuevoRfc))
                    return BadRequest(new { message = "Tickets y NuevoRfc son requeridos" });

                var sql = @"
                    -- 1) Actualizar en facturacion_recepciones
                    UPDATE fr
                    SET fr.rfc_productor = @NuevoRfc,
                        fr.updated_at    = GETDATE()
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero IN @Tickets
                      AND (@SedeId = 0 OR fr.sede_id = @SedeId);

                    -- 2) Actualizar en productores (fuente maestra)
                    UPDATE p
                    SET p.rfc = @NuevoRfc
                    FROM productores p
                    INNER JOIN bascula_recepciones br ON br.productor_id = p.id
                    INNER JOIN boletas b ON b.bascula_id = br.id
                    WHERE b.ticket_numero IN @Tickets;";

                var affected = await _db.ExecuteAsync(sql, new { dto.Tickets, dto.NuevoRfc, dto.SedeId });
                return Ok(new { success = true, registrosAfectados = affected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar RFC", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  PUT /facturacion/actualizar-documentos-status
        //  Actualiza tiene_documentos y tiene_factura_xml
        // ═══════════════════════════════════════════════
        [HttpPut("actualizar-documentos-status")]
        public async Task<IActionResult> ActualizarDocumentosStatus([FromBody] ActualizarDocsStatusRequest dto)
        {
            try
            {
                if (dto.Tickets.Length == 0)
                    return BadRequest(new { message = "Tickets es requerido" });

                var sql = @"
                    UPDATE fr
                    SET fr.tiene_documentos  = @TieneDocumentos,
                        fr.tiene_factura_xml = @TieneFacturaXml,
                        fr.updated_at        = GETDATE()
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero IN @Tickets
                      AND (@SedeId = 0 OR fr.sede_id = @SedeId);";

                var affected = await _db.ExecuteAsync(sql, new
                {
                    dto.Tickets,
                    dto.TieneDocumentos,
                    dto.TieneFacturaXml,
                    dto.SedeId
                });

                return Ok(new { success = true, registrosAfectados = affected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar status de documentos", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  POST /facturacion/enviar-a-pagos
        //  1) Cambia status → 'ENVIADO_A_PAGOS'
        //  2) Crea registro en solicitudes_pago con status 'SOLICITAR'
        // ═══════════════════════════════════════════════
        [HttpPost("enviar-a-pagos")]
        public async Task<IActionResult> EnviarAPagos([FromBody] EnviarAPagosRequest dto)
        {
            try
            {
                if (dto.Tickets.Length == 0)
                    return BadRequest(new { message = "Debe enviar al menos un ticket" });

                // ── 1. Obtener recepciones que cumplen condiciones ────────────
                var sqlGetRecepciones = @"
                    SELECT fr.id, ISNULL(fr.monto_total, fr.importe_factura) AS monto, fr.sede_id
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero IN @Tickets
                      AND (@SedeId = 0 OR fr.sede_id = @SedeId)
                      AND fr.tiene_documentos  = 1
                      AND fr.tiene_factura_xml = 1
                      AND ISNULL(fr.rfc_productor, '') <> ''
                      AND fr.rfc_productor <> 'Pendiente RFC'
                      AND fr.status <> 'ENVIADO_A_PAGOS';";

                var recepciones = (await _db.QueryAsync<dynamic>(
                    sqlGetRecepciones, new { dto.Tickets, dto.SedeId })).ToList();

                if (!recepciones.Any())
                    return BadRequest(new { message = "Ningún registro cumple las condiciones para enviar a pagos" });

                // ── 2. Ejecutar dentro de una transacción ─────────────────────
                if (_db.State != System.Data.ConnectionState.Open) _db.Open();
                using var tx = _db.BeginTransaction();
                try
                {
                    var ids = recepciones.Select(r => (int)r.id).ToArray();

                    // 2a. Actualizar status en facturacion_recepciones
                    var sqlUpdate = @"
                        UPDATE facturacion_recepciones
                        SET status     = 'ENVIADO_A_PAGOS',
                            updated_at = GETDATE()
                        WHERE id IN @Ids;";

                    await _db.ExecuteAsync(sqlUpdate, new { Ids = ids }, tx);

                    // 2b. Crear solicitudes_pago (si no existe ya una para ese facturacion_id)
                    var sqlInsert = @"
                        INSERT INTO solicitudes_pago
                            (facturacion_id, monto_solicitado, prioridad, status, sede_id, created_at, updated_at)
                        SELECT @FacturacionId, @Monto, 0, 'SOLICITAR', @SedeId, GETDATE(), GETDATE()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM solicitudes_pago WHERE facturacion_id = @FacturacionId
                        );";

                    foreach (var r in recepciones)
                    {
                        await _db.ExecuteAsync(sqlInsert, new
                        {
                            FacturacionId = (int)r.id,
                            Monto         = (decimal)(r.monto ?? 0),
                            SedeId        = (int)r.sede_id
                        }, tx);
                    }

                    tx.Commit();
                    return Ok(new { success = true, registrosAfectados = recepciones.Count });
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al enviar a pagos", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  GET /facturacion/detalle-ticket?ticket=1&sedeId=8
        //  Retorna boleta + preliquidación + báscula + análisis + pago
        // ═══════════════════════════════════════════════
        [HttpGet("detalle-ticket")]
        public async Task<IActionResult> GetDetalleTicket([FromQuery] string ticket, [FromQuery] int sedeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticket))
                    return BadRequest(new { message = "El ticket es requerido" });

                var sql = @"
                    SELECT
                        -- Identificación
                        b.ticket_numero                                                 AS ticket,
                        FORMAT(ISNULL(fr.fecha_recepcion, fr.created_at), 'dd/MM/yyyy') AS fecha,
                        FORMAT(br.fecha_hora, 'dd/MM/yyyy HH:mm')                       AS fechaHora,

                        -- Productor
                        ISNULL(p.nombre, bp.productor_nombre)                           AS productor,
                        ISNULL(
                            NULLIF(fr.rfc_productor, ''),
                            ISNULL(NULLIF(p.rfc, ''), 'Pendiente RFC')
                        )                                                               AS rfc,
                        ISNULL(p.tipo_persona, '')                                      AS tProductor,
                        ISNULL(p.telefono, bp.telefono)                                 AS telefono,
                        ISNULL(p.atiende, '')                                           AS atiende,
                        ISNULL(p.correo, '')                                            AS correo,
                        ISNULL(bp.origen, '')                                           AS municipio,

                        -- Boleta / calidad
                        ISNULL(bp.no_boleta, '')                                        AS folio,
                        bp.comprador                                                    AS comprador,
                        bp.origen                                                       AS origen,
                        ISNULL(ac.calibre, bp.calibre)                                  AS calibre,
                        ISNULL(ac.humedad, 0)                                           AS humedad,
                        ISNULL(ac.impurezas, 0)                                         AS impurezas,
                        ISNULL(ac.r1_danado_insecto, 0)                                 AS r1,
                        ISNULL(ac.r2_arrugado, 0)                                       AS r2,
                        ISNULL(ac.r2_manchado, 0)                                       AS r2Manchado,
                        ISNULL(ac.r2_quebrado, 0)                                       AS r2Quebrado,
                        ISNULL(ac.suma_r2, 0)                                           AS sumaR2,
                        ISNULL(ac.total_danos, 0)                                       AS totalDanos,

                        -- Producto (grano)
                        ISNULL(g.nombre, '')                                            AS producto,

                        -- Báscula
                        br.chofer                                                       AS chofer,
                        br.placas                                                       AS placas,
                        CAST(ISNULL(br.peso_bruto_kg, 0) AS DECIMAL(12,2))              AS pesoBruto,
                        CAST(ISNULL(br.tara_kg, 0) AS DECIMAL(12,2))                    AS tara,
                        CAST(ISNULL(br.peso_neto_kg, 0) AS DECIMAL(12,2))               AS pesoNeto,

                        -- Preliquidación
                        ISNULL(fr.precio_promedio,
                            ISNULL(pl.precio_base_mxn_ton,
                                ISNULL(bp.precio_final, 0)
                            )
                        )                                                               AS precio,
                        ISNULL(pl.descuento_kg_ton, ISNULL(bp.descuento_kg_ton, 0))     AS descuento,
                        ISNULL(pl.kg_a_liquidar, 0)                                     AS kgLiquidar,
                        ISNULL(pl.observaciones, '')                                     AS observaciones,

                        -- Facturación / Pago
                        ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) AS importe,
                        CAST(
                            ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) * 0.987
                        AS DECIMAL(18,2))                                               AS aPagar,
                        fr.uuid_fiscal                                                  AS folioFiscal,
                        fr.serie                                                        AS serie,
                        fr.folio                                                        AS folioFactura,
                        fr.status,
                        ISNULL(fr.tiene_documentos, 0)                                  AS tieneDocumentos,
                        ISNULL(fr.tiene_factura_xml, 0)                                 AS tieneFacturaXML,
                        br.grano_id                                                     AS granoId,
                        ac.datos_adicionales                                            AS analisisDatosAdicionales

                    FROM dbo.facturacion_recepciones fr
                    LEFT JOIN dbo.preliquidaciones    pl  ON fr.preliquidacion_id = pl.id
                    LEFT JOIN dbo.boletas             b   ON fr.boleta_id = b.id
                    LEFT JOIN dbo.bascula_recepciones br  ON b.bascula_id = br.id
                    LEFT JOIN dbo.productores         p   ON ISNULL(br.productor_id, fr.productor_id) = p.id
                    LEFT JOIN dbo.boletas_precio      bp  ON bp.boleta_id = b.id
                    LEFT JOIN dbo.analisis_calidad    ac  ON b.analisis_id = ac.id
                    LEFT JOIN dbo.granos_catalogo     g   ON br.grano_id = g.id
                    WHERE b.ticket_numero = @ticket
                      AND (@sedeId = 0 OR fr.sede_id = @sedeId)";

                var detalle = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { ticket, sedeId });

                if (detalle == null)
                    return NotFound(new { message = $"No se encontró el ticket {ticket}" });

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener detalle", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  POST /facturacion/guardar-xml
        //  Guarda el XML de la factura como Base64 en datos_adicionales
        //  y actualiza importe_factura + tiene_factura_xml
        // ═══════════════════════════════════════════════
        [HttpPost("guardar-xml")]
        public async Task<IActionResult> GuardarXml([FromBody] GuardarXmlRequest dto)
        {
            try
            {
                if (dto.Tickets.Length == 0)
                    return BadRequest(new { message = "Tickets es requerido" });

                await EnsurarColumnaDatosAdicionales();

                // Leer datos_adicionales actuales del primer ticket y fusionar
                var existingJson = await _db.QueryFirstOrDefaultAsync<string>(@"
                    SELECT TOP 1 fr.datos_adicionales
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero = @ticket",
                    new { ticket = dto.Tickets[0] });

                var root = string.IsNullOrWhiteSpace(existingJson)
                    ? new JsonObject()
                    : JsonNode.Parse(existingJson)?.AsObject() ?? new JsonObject();

                root["xml"] = JsonSerializer.SerializeToNode(new
                {
                    nombre = dto.XmlNombre ?? "",
                    base64 = dto.XmlBase64 ?? "",
                    importe = dto.Importe,
                    pagoPredial = dto.PagoPredial,
                    descPredial = dto.DescPredial,
                    descISR = dto.DescISR,
                    diasHabilesPago = dto.DiasHabilesPago,
                    fechaGuardado = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                });

                var mergedJson = root.ToJsonString();
                decimal.TryParse(dto.Importe?.Replace(",", "").Replace("$", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var importeNum);

                var sql = @"
                    UPDATE fr
                    SET fr.tiene_factura_xml = 1,
                        fr.importe_factura   = CASE WHEN @ImporteNum > 0 THEN @ImporteNum ELSE fr.importe_factura END,
                        fr.datos_adicionales = @MergedJson,
                        fr.updated_at        = GETDATE()
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero IN @Tickets
                      AND (@SedeId = 0 OR fr.sede_id = @SedeId);";

                await _db.ExecuteAsync(sql, new { dto.Tickets, ImporteNum = importeNum, MergedJson = mergedJson, dto.SedeId });
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar XML", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  POST /facturacion/guardar-expediente
        //  Guarda todos los documentos del expediente como Base64
        //  en datos_adicionales (JSON) y actualiza flags
        // ═══════════════════════════════════════════════
        
        // ═══════════════════════════════════════════════
        //  GET /facturacion/documentos-expediente?ticket=X
        //  Retorna los datos_adicionales (JSON con Base64) de un ticket
        // ═══════════════════════════════════════════════
        [HttpGet("documentos-expediente")]
        public async Task<IActionResult> GetDocumentosExpediente([FromQuery] string ticket, [FromQuery] int sedeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticket))
                    return BadRequest(new { message = "El ticket es requerido" });

                await EnsurarColumnaDatosAdicionales();

                var sql = @"
                    SELECT ISNULL(fr.datos_adicionales, '{}') AS datos
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero = @ticket
                      AND (@sedeId = 0 OR fr.sede_id = @sedeId)";

                var jsonStr = await _db.QueryFirstOrDefaultAsync<string>(sql, new { ticket, sedeId });
                if (jsonStr == null)
                    return NotFound(new { message = $"No se encontró el ticket {ticket}" });

                // Devolver JSON crudo sin doble-serialización
                return Content(jsonStr, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener documentos", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        //  POST /facturacion/eliminar-documento
        //  Elimina un campo específico del JSON datos_adicionales
        // ═══════════════════════════════════════════════
        [HttpPost("eliminar-documento")]
        public async Task<IActionResult> EliminarDocumento([FromBody] EliminarDocumentoRequest dto)
        {
            try
            {
                if (dto.Tickets.Length == 0 || string.IsNullOrWhiteSpace(dto.Tipo))
                    return BadRequest(new { message = "Tickets y Tipo son requeridos" });

                await EnsurarColumnaDatosAdicionales();

                var existingJson = await _db.QueryFirstOrDefaultAsync<string>(@"
                    SELECT TOP 1 fr.datos_adicionales
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero = @ticket
                      AND (@sedeId = 0 OR fr.sede_id = @sedeId)",
                    new { ticket = dto.Tickets[0], sedeId = dto.SedeId });

                var root = string.IsNullOrWhiteSpace(existingJson)
                    ? new JsonObject()
                    : JsonNode.Parse(existingJson)?.AsObject() ?? new JsonObject();

                root.Remove(dto.Tipo);
                var mergedJson = root.ToJsonString();

                // Si se elimina el XML, resetear el flag tiene_factura_xml
                var resetXml = dto.Tipo == "xml" ? 1 : 0;

                await _db.ExecuteAsync(@"
                    UPDATE fr
                    SET fr.datos_adicionales = @MergedJson,
                        fr.tiene_factura_xml = CASE WHEN @ResetXml = 1 THEN 0 ELSE fr.tiene_factura_xml END,
                        fr.updated_at        = GETDATE()
                    FROM facturacion_recepciones fr
                    INNER JOIN boletas b ON fr.boleta_id = b.id
                    WHERE b.ticket_numero IN @Tickets
                      AND (@SedeId = 0 OR fr.sede_id = @SedeId);",
                    new { dto.Tickets, MergedJson = mergedJson, dto.SedeId, ResetXml = resetXml });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar documento", detail = ex.Message });
            }
        }

        // Asegura que la columna datos_adicionales exista en facturacion_recepciones
        private async Task EnsurarColumnaDatosAdicionales()
        {
            await _db.ExecuteAsync(@"
                IF COL_LENGTH('facturacion_recepciones', 'datos_adicionales') IS NULL
                    ALTER TABLE facturacion_recepciones ADD datos_adicionales NVARCHAR(MAX) NULL;");
        }

        // ═══════════════════════════════════════════════
        //  GET /facturacion/exportar-excel?sedeId=8
        //  Genera archivo .xlsx con ClosedXML
        // ═══════════════════════════════════════════════
        [HttpGet("exportar-excel")]
        public async Task<IActionResult> ExportarExcel([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        ISNULL(b.ticket_numero, CAST(pl.ticket_numero AS VARCHAR)) AS Ticket,
                        FORMAT(ISNULL(fr.fecha_recepcion, fr.created_at), 'dd/MM/yyyy') AS Fecha,
                        ISNULL(
                            NULLIF(fr.rfc_productor, ''),
                            ISNULL(NULLIF(p.rfc, ''), 'Pendiente RFC')
                        ) AS RFC,
                        ISNULL(
                            CASE
                                WHEN p.tipo_persona = 'Moral' THEN ISNULL(p.atiende, p.nombre)
                                ELSE p.nombre
                            END,
                            bp.productor_nombre
                        ) AS Productor,
                        CAST(
                            ISNULL(fr.kg_total_entregados,
                                ISNULL(pl.kg_a_liquidar, ISNULL(pl.peso_neto_kg, 0))
                            ) / 1000.0
                        AS DECIMAL(10,3)) AS Toneladas,
                        ISNULL(fr.kg_total_entregados, ISNULL(pl.kg_a_liquidar, 0)) AS NetoAPagar,
                        ISNULL(fr.precio_promedio,
                            ISNULL(pl.precio_base_mxn_ton, ISNULL(bp.precio_final, 0))
                        ) AS Precio,
                        ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) AS Importe,
                        CAST(
                            ISNULL(fr.importe_factura, ISNULL(pl.importe_total, fr.monto_total)) * 0.987
                        AS DECIMAL(18,2)) AS APagar,
                        fr.status AS Status
                    FROM dbo.facturacion_recepciones fr
                    LEFT JOIN dbo.preliquidaciones    pl ON fr.preliquidacion_id = pl.id
                    LEFT JOIN dbo.boletas             b  ON fr.boleta_id = b.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.productores         p  ON ISNULL(br.productor_id, fr.productor_id) = p.id
                    LEFT JOIN dbo.boletas_precio      bp ON bp.boleta_id = b.id
                    WHERE (@sedeId = 0 OR fr.sede_id = @sedeId)
                    ORDER BY fr.created_at DESC";

                var rows = (await _db.QueryAsync<dynamic>(sql, new { sedeId })).ToList();

                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Recepción Facturas");

                // Encabezados
                string[] headers = { "Ticket", "Fecha", "RFC", "Productor", "Toneladas", "Neto a Pagar (KG)", "Precio", "Importe", "A Pagar", "Status" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Datos
                int row = 2;
                foreach (var r in rows)
                {
                    ws.Cell(row, 1).Value = (string)(r.Ticket ?? "");
                    ws.Cell(row, 2).Value = (string)(r.Fecha ?? "");
                    ws.Cell(row, 3).Value = (string)(r.RFC ?? "");
                    ws.Cell(row, 4).Value = (string)(r.Productor ?? "");
                    ws.Cell(row, 5).Value = (decimal)(r.Toneladas ?? 0m);
                    ws.Cell(row, 6).Value = (decimal)(r.NetoAPagar ?? 0m);
                    ws.Cell(row, 7).Value = (decimal)(r.Precio ?? 0m);
                    ws.Cell(row, 8).Value = (decimal)(r.Importe ?? 0m);
                    ws.Cell(row, 9).Value = (decimal)(r.APagar ?? 0m);
                    ws.Cell(row, 10).Value = (string)(r.Status ?? "");

                    ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";

                    if ((string)(r.RFC ?? "") == "Pendiente RFC")
                        ws.Cell(row, 3).Style.Font.FontColor = XLColor.Red;

                    row++;
                }

                ws.Columns().AdjustToContents();

                if (rows.Count > 0)
                {
                    ws.Cell(row, 4).Value = "TOTALES";
                    ws.Cell(row, 4).Style.Font.Bold = true;
                    ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
                    ws.Cell(row, 8).FormulaA1 = $"SUM(H2:H{row - 1})";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 9).FormulaA1 = $"SUM(I2:I{row - 1})";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Row(row).Style.Font.Bold = true;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"facturacion_recepciones_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar", detail = ex.Message });
            }
        }
    
    
   
        [HttpPost("validar-identidad")]
        public IActionResult ValidarIdentidad([FromQuery] int sedeId, [FromBody] ValidacionIdentidadRequest request)
        {
            _logger.LogInformation("Iniciando validación de identidad para el productor: {Nombre}", request.NombreProductorEsperado);

            if (string.IsNullOrEmpty(request.TextoOcr))
            {
                return BadRequest(new { message = "El texto extraído del OCR está vacío." });
            }

            try
            {
                // 1. Limpieza básica del texto recibido
                string textoOcr = request.TextoOcr.ToUpper();
                string nombreEsperado = request.NombreProductorEsperado.ToUpper();

                // 2. Lógica de validación: Buscamos si el nombre del ERP existe en el texto del OCR
                // Dividimos el nombre esperado en palabras (Elias, Villegas, etc.)
                var palabrasNombre = nombreEsperado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int coincidencias = 0;

                foreach (var palabra in palabrasNombre)
                {
                    if (textoOcr.Contains(palabra))
                    {
                        coincidencias++;
                    }
                }

                // Consideramos válido si coinciden al menos 2 palabras (ej. un nombre y un apellido)
                bool esValido = coincidencias >= 2;

                return Ok(new
                {
                    success = true,
                    coincide = esValido,
                    palabrasEncontradas = coincidencias,
                    mensaje = esValido 
                        ? "Identidad verificada correctamente." 
                        : "El nombre en el documento no parece coincidir con el productor registrado."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la validación de texto.");
                return StatusCode(500, new { message = "Error interno en el servidor de Alazan." });
            }
        }
    
        [HttpPost("validar-y-guardar")]
        public async Task<IActionResult> ValidarYGuardar([FromBody] RegistroOcrDto dto)
        {
            _logger.LogInformation("Guardando documento PDF en datos_adicionales para: {Productor}", dto.NombreProductorEsperado);

            try
            {
                // Consultamos los registros actuales
                string sqlSelect = "SELECT id, datos_adicionales FROM Facturacion_Recepciones WHERE folio IN @Tickets AND sede_id = @SedeId";
                var registros = await _db.QueryAsync<dynamic>(sqlSelect, new { dto.Tickets, dto.SedeId });

                foreach (var reg in registros)
                {
                    string jsonActual = reg.datos_adicionales ?? "{}";
                    var node = JsonNode.Parse(jsonActual);

                    // Guardamos la sección de identificación sin importar el resultado del OCR
                    node!["identificacion"] = new JsonObject
                    {
                        ["nombre"] = dto.NombreArchivo,
                        ["base64"] = dto.ArchivoBase64,
                        ["fechaGuardado"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                        ["estatus"] = "CARGADO" 
                    };

                    string sqlUpdate = "UPDATE Facturacion_Recepciones SET datos_adicionales = @NuevoJson, tiene_documentos = 1 WHERE id = @Id";
                    await _db.ExecuteAsync(sqlUpdate, new { NuevoJson = node.ToJsonString(), Id = reg.id });
                }

                return Ok(new { coincide = true, message = "Documento guardado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar identificación");
                return StatusCode(500, new { message = "Error al persistir el documento: " + ex.Message });
            }
        }


        [HttpPost("guardar-expediente")]
        public async Task<IActionResult> GuardarExpediente([FromBody] GuardarExpedienteRequest request)
        {
            try
            {
                // 1. ACTUALIZAR DATOS DEL PRODUCTOR (Correo y Teléfono)
                // Buscamos el ProductorId asociado a estos tickets
                string sqlProductor = @"
                    UPDATE Productores 
                    SET Correo = @Correo, 
                        Telefono = @Telefono
                    WHERE Id = (SELECT TOP 1 productor_id FROM Facturacion_Recepciones WHERE id = @Ticket AND sede_id = @SedeId)";

                await _db.ExecuteAsync(sqlProductor, new { 
                    Correo = request.Correo, 
                    Telefono = request.Telefono, 
                    Ticket = request.Tickets.FirstOrDefault(),
                    SedeId = request.SedeId 
                });

                // 2. ACTUALIZAR EXPEDIENTE EN DATOS_ADICIONALES
                string sqlSelect = "SELECT id, datos_adicionales FROM Facturacion_Recepciones WHERE id IN @Tickets AND sede_id = @SedeId";
                var registros = await _db.QueryAsync<dynamic>(sqlSelect, new { request.Tickets, request.SedeId });

                foreach (var reg in registros)
                {
                    // Mantenemos lo que ya existe (como el XML) y agregamos lo nuevo
                    string jsonActual = reg.datos_adicionales ?? "{}";
                    var node = JsonNode.Parse(jsonActual);

                    // Estructura de documentos
                    if (!string.IsNullOrEmpty(request.IdentificacionBase64))
                        node!["identificacion"] = new JsonObject { ["nombre"] = request.IdentificacionNombre, ["base64"] = request.IdentificacionBase64 };
                    
                    if (!string.IsNullOrEmpty(request.ConstanciaBase64))
                        node!["constancia"] = new JsonObject { ["nombre"] = request.ConstanciaNombre, ["base64"] = request.ConstanciaBase64, ["fecha"] = request.FechaConstancia };

                    if (!string.IsNullOrEmpty(request.OpinionBase64))
                        node!["opinion"] = new JsonObject { ["nombre"] = request.OpinionNombre, ["base64"] = request.OpinionBase64, ["fecha"] = request.FechaOpinion };

                    // Datos de contacto adicional para el expediente
                    node!["contacto_adicional"] = new JsonObject {
                        ["nombre"] = request.Nombre,
                        ["telefono"] = request.Telefono,
                        ["correo"] = request.Correo
                    };

                    string sqlUpdate = "UPDATE Facturacion_Recepciones SET datos_adicionales = @NuevoJson, tiene_documentos = 1 WHERE id = @Id";
                    await _db.ExecuteAsync(sqlUpdate, new { NuevoJson = node.ToJsonString(), Id = reg.id });
                }

                return Ok(new { success = true, message = "Productor y Expediente actualizados correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar expediente completo");
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class ValidacionIdentidadRequest
    {
        public string TextoOcr { get; set; } = string.Empty;
        public string NombreProductorEsperado { get; set; } = string.Empty;
    }

    public class EliminarDocumentoRequest
    {
        public string[] Tickets { get; set; } = [];
        public int SedeId { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    }
    

