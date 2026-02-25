using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using SistemaAlazan.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ClosedXML.Excel;

namespace Alazan.API.Controllers
{
    /*
     * ════════════════════════════════════════════════════════════════════════
     *  MIGRACIÓN SQL – ejecutar en SQL Server ANTES de usar este módulo
     * ════════════════════════════════════════════════════════════════════════
     *
     *  -- Ampliar tabla solicitudes_pago con columnas del workflow de pagos
     *  ALTER TABLE solicitudes_pago ADD
     *      sede_id                 INT             NULL,
     *      banco_id                INT             NULL,
     *      forma_pago_id           INT             NULL,
     *      clabe                   NVARCHAR(18)    NULL,
     *      cuenta                  NVARCHAR(50)    NULL,
     *      fecha_solicitud         DATETIME        NULL,
     *      fecha_autorizacion      DATETIME        NULL,
     *      fecha_pago              DATETIME        NULL,
     *      folio_pago              NVARCHAR(50)    NULL,
     *      usuario_solicitud_id    INT             NULL,
     *      usuario_autorizacion_id INT             NULL,
     *      usuario_pago_id         INT             NULL;
     *
     *  NOTA: tope_diario ya existe en sedes_catalogo. No requiere migración adicional.
     *
     *  -- FKs opcionales
     *  -- ALTER TABLE solicitudes_pago ADD CONSTRAINT FK_sp_banco
     *  --     FOREIGN KEY (banco_id) REFERENCES bancos_catalogo(id);
     *  -- ALTER TABLE solicitudes_pago ADD CONSTRAINT FK_sp_forma
     *  --     FOREIGN KEY (forma_pago_id) REFERENCES Configuracion_Pagos_Formas(id);
     * ════════════════════════════════════════════════════════════════════════
     */

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<PagosController> _logger;

        public PagosController(IDbConnection db, ILogger<PagosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/configuracion-completa?sedeId=8
        //  Retorna config general + status + formas de pago + bancos
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("configuracion-completa")]
        public async Task<IActionResult> GetConfiguracionCompleta([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT * FROM Configuracion_Pagos_General  WHERE sede_id = @sedeId;
                    SELECT * FROM Configuracion_Pagos_Status   WHERE sede_id = @sedeId AND activo = 1 ORDER BY orden;
                    SELECT * FROM Configuracion_Pagos_Formas   WHERE sede_id = @sedeId AND activo = 1;
                    SELECT * FROM Configuracion_Pagos_Dias     WHERE sede_id = @sedeId;
                    SELECT id, nombre_banco, codigo_banco
                    FROM bancos_catalogo
                    WHERE activo = 1 AND (sede_id = @sedeId OR sede_id IS NULL)
                    ORDER BY nombre_banco;";

                using var multi = await _db.QueryMultipleAsync(sql, new { sedeId });
                var response = new
                {
                    General     = await multi.ReadFirstOrDefaultAsync<dynamic>(),
                    Status      = await multi.ReadAsync<dynamic>(),
                    FormasPago  = await multi.ReadAsync<dynamic>(),
                    DiasHabiles = await multi.ReadAsync<dynamic>(),
                    Bancos      = await multi.ReadAsync<dynamic>()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/tope-diario?sedeId=8
        //  tope_diario viene de sedes_catalogo; solicitado_hoy de solicitudes_pago
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("tope-diario")]
        public async Task<IActionResult> GetTopeDiario([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT ISNULL(tope_diario, 0) AS tope_diario
                    FROM sedes_catalogo
                    WHERE id = @sedeId;

                    SELECT ISNULL(SUM(sp.monto_solicitado), 0) AS solicitado_hoy
                    FROM solicitudes_pago sp
                    WHERE sp.sede_id = @sedeId
                      AND sp.status  = 'PAGO SOLICITADO'
                      AND CAST(sp.created_at AS DATE) = CAST(GETDATE() AS DATE);";

                using var multi = await _db.QueryMultipleAsync(sql, new { sedeId });
                var config  = await multi.ReadFirstOrDefaultAsync<dynamic>();
                var metrica = await multi.ReadFirstOrDefaultAsync<dynamic>();

                decimal topeDiario    = (decimal)(config?.tope_diario ?? 0m);
                decimal solicitadoHoy = (decimal)(metrica?.solicitado_hoy ?? 0m);

                return Ok(new
                {
                    topeDiario,
                    solicitadoHoy,
                    disponible          = topeDiario - solicitadoHoy,
                    porcentajeUtilizado = topeDiario > 0 ? Math.Round((solicitadoHoy / topeDiario) * 100, 1) : 0m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/topes-sedes
        //  Vista CORPORATIVA: tope diario + uso actual de TODAS las sedes
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("topes-sedes")]
        public async Task<IActionResult> GetTopesSedes()
        {
            try
            {
                var sql = @"
                    SELECT
                        sc.id,
                        sc.nombre_sede,
                        sc.ciudad,
                        sc.estado,
                        ISNULL(sc.tope_diario, 0)                               AS tope_diario,
                        ISNULL(SUM(CASE
                            WHEN sp.status = 'PAGO SOLICITADO'
                             AND CAST(sp.created_at AS DATE) = CAST(GETDATE() AS DATE)
                            THEN sp.monto_solicitado ELSE 0 END), 0)             AS solicitado_hoy
                    FROM sedes_catalogo sc
                    LEFT JOIN solicitudes_pago sp ON sp.sede_id = sc.id
                    WHERE sc.activo = 1
                    GROUP BY sc.id, sc.nombre_sede, sc.ciudad, sc.estado, sc.tope_diario
                    ORDER BY sc.nombre_sede;";

                var rows = await _db.QueryAsync<dynamic>(sql);
                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/solicitudes?sedeId=8
        //  Vista SEDE: sus propias solicitudes (todos los status)
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("solicitudes")]
        public async Task<IActionResult> GetSolicitudes([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        sp.id,
                        sp.facturacion_id,
                        sp.monto_solicitado,
                        sp.status                                   AS status_pago,
                        FORMAT(sp.fecha_solicitud,   'dd/MM/yyyy')  AS fecha_solicitud,
                        FORMAT(sp.fecha_autorizacion,'dd/MM/yyyy')  AS fecha_autorizacion,
                        FORMAT(sp.fecha_pago,        'dd/MM/yyyy')  AS fecha_pago,
                        sp.folio_pago,
                        sp.banco_id,
                        bc.nombre_banco,
                        sp.metodo_pago,
                        cpf.nombre                                  AS nombre_forma_pago,
                        sp.cuenta_clabe,
                        -- Ticket y entrega
                        b.ticket_numero                             AS ticket,
                        FORMAT(fr.fecha_recepcion, 'dd/MM/yyyy')    AS fecha_entrega,
                        fr.rfc_productor,
                        fr.kg_total_entregados / 1000.0             AS toneladas,
                        fr.precio_promedio,
                        fr.importe_factura,
                        fr.sede_id,
                        -- Productor (pre-carga CLABE y banco)
                        p.nombre                                    AS nombre_productor,
                        p.cuenta_clabe                              AS cuenta_clabe_productor,
                        p.banco_id                                  AS banco_id_productor,
                        bc_prod.nombre_banco                        AS banco_productor
                    FROM solicitudes_pago sp
                    INNER JOIN facturacion_recepciones fr      ON sp.facturacion_id = fr.id
                    INNER JOIN boletas b                       ON fr.boleta_id       = b.id
                    INNER JOIN productores p                   ON fr.productor_id    = p.id
                    LEFT  JOIN bancos_catalogo bc              ON sp.banco_id        = bc.id
                    LEFT  JOIN Configuracion_Pagos_Formas cpf  ON sp.metodo_pago   = cpf.id
                    LEFT  JOIN bancos_catalogo bc_prod         ON p.banco_id         = bc_prod.id
                    WHERE (@sedeId = 0 OR fr.sede_id = @sedeId)
                    ORDER BY sp.created_at DESC;";

                var rows = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/solicitudes-sede?sedeId=8
        //  Vista CORPORATIVA: PAGO SOLICITADO / AUTORIZADO / PAGADO
        //  sedeId = 0 → todas las sedes activas
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("solicitudes-sede")]
        public async Task<IActionResult> GetSolicitudesSede([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        sp.id,
                        sp.facturacion_id,
                        sp.monto_solicitado,
                        sp.status                                   AS status_pago,
                        FORMAT(sp.fecha_solicitud,   'dd/MM/yyyy')  AS fecha_solicitud,
                        FORMAT(sp.fecha_autorizacion,'dd/MM/yyyy')  AS fecha_autorizacion,
                        FORMAT(sp.fecha_pago,        'dd/MM/yyyy')  AS fecha_pago,
                        sp.folio_pago,
                        sp.banco_id,
                        bc.nombre_banco,
                        sp.metodo_pago,
                        cpf.nombre                                  AS nombre_forma_pago,
                        sp.cuenta_clabe,
                        b.ticket_numero                             AS ticket,
                        FORMAT(fr.fecha_recepcion, 'dd/MM/yyyy')    AS fecha_entrega,
                        fr.rfc_productor,
                        fr.kg_total_entregados / 1000.0             AS toneladas,
                        fr.precio_promedio,
                        fr.importe_factura,
                        fr.sede_id,
                        sc.nombre_sede,
                        p.nombre                                    AS nombre_productor
                    FROM solicitudes_pago sp
                    INNER JOIN facturacion_recepciones fr      ON sp.facturacion_id = fr.id
                    INNER JOIN boletas b                       ON fr.boleta_id       = b.id
                    INNER JOIN productores p                   ON fr.productor_id    = p.id
                    LEFT  JOIN sedes_catalogo sc               ON fr.sede_id         = sc.id
                    LEFT  JOIN bancos_catalogo bc              ON sp.banco_id        = bc.id
                    LEFT  JOIN Configuracion_Pagos_Formas cpf  ON sp.metodo_pago   = cpf.id
                    WHERE sp.status IN ('PAGO SOLICITADO', 'AUTORIZADO', 'PAGADO')
                      AND (@sedeId = 0 OR fr.sede_id = @sedeId)
                    ORDER BY sp.created_at DESC;";

                var rows = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PUT /pagos/actualizar-datos-bancarios
        //  Actualiza banco, forma de pago y CLABE (solo en estado SOLICITAR)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPut("actualizar-datos-bancarios")]
        public async Task<IActionResult> ActualizarDatosBancarios([FromBody] ActualizarDatosBancariosRequest dto)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var trans = _db.BeginTransaction();
            try
            {
                // 1. Actualizar solicitud de pago
                var sqlSolicitud = @"
                    UPDATE solicitudes_pago
                    SET banco_id     = @BancoId,
                        metodo_pago  = @FormaPagoId,
                        cuenta_clabe = @Clabe,
                        updated_at   = GETDATE()
                    WHERE id     = @SolicitudId
                      AND status = 'SOLICITAR';";

                var affected = await _db.ExecuteAsync(sqlSolicitud, dto, transaction: trans);
                if (affected == 0)
                {
                    trans.Rollback();
                    return BadRequest(new { message = "No se actualizó. Verifique que el pago esté en estado SOLICITAR." });
                }

                // 2. Sincronizar banco y CLABE en el perfil del productor
                // (productor_id puede ser NULL en solicitudes_pago, se llega vía facturacion_recepciones)
                var sqlProductor = @"
                    UPDATE p
                    SET p.banco_id     = @BancoId,
                        p.cuenta_clabe = @Clabe
                    FROM productores p
                    INNER JOIN facturacion_recepciones fr ON fr.productor_id = p.id
                    INNER JOIN solicitudes_pago sp        ON sp.facturacion_id = fr.id
                    WHERE sp.id = @SolicitudId;";

                await _db.ExecuteAsync(sqlProductor, dto, transaction: trans);

                trans.Commit();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, new { message = ex.Message });
            }
            finally
            {
                _db.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  POST /pagos/solicitar-pago
        //  Cambia SOLICITAR → PAGO SOLICITADO (valida tope en sedes_catalogo)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("solicitar-pago")]
        public async Task<IActionResult> SolicitarPago([FromBody] SolicitarPagoRequest dto)
        {
            try
            {
                if (dto.SolicitudIds == null || dto.SolicitudIds.Length == 0)
                    return BadRequest(new { message = "Debe seleccionar al menos una solicitud" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // ── Tope diario desde sedes_catalogo ────────────────────────
                var topeDiario = await _db.ExecuteScalarAsync<decimal>(
                    "SELECT ISNULL(tope_diario, 0) FROM sedes_catalogo WHERE id = @sedeId",
                    new { dto.SedeId });

                // ── Monto ya solicitado hoy ──────────────────────────────────
                var solicitadoHoy = await _db.ExecuteScalarAsync<decimal>(@"
                    SELECT ISNULL(SUM(monto_solicitado), 0)
                    FROM solicitudes_pago
                    WHERE sede_id = @sedeId
                      AND status  = 'PAGO SOLICITADO'
                      AND CAST(fecha_solicitud AS DATE) = CAST(GETDATE() AS DATE)",
                    new { dto.SedeId });

                // ── Monto de los IDs seleccionados ───────────────────────────
                var montoSeleccionado = await _db.ExecuteScalarAsync<decimal>(@"
                    SELECT ISNULL(SUM(monto_solicitado), 0)
                    FROM solicitudes_pago
                    WHERE id IN @Ids AND status = 'SOLICITAR'",
                    new { Ids = dto.SolicitudIds });

                // ── Validar tope (solo si hay tope configurado) ──────────────
                if (topeDiario > 0 && (solicitadoHoy + montoSeleccionado) > topeDiario)
                {
                    return BadRequest(new
                    {
                        message           = "Tope diario excedido",
                        topeDiario,
                        solicitadoHoy,
                        montoSeleccionado,
                        excedente         = (solicitadoHoy + montoSeleccionado) - topeDiario
                    });
                }

                // ── Cambiar status ───────────────────────────────────────────
                var sql = @"
                    UPDATE solicitudes_pago
                    SET status               = 'PAGO SOLICITADO',
                        fecha_solicitud      = GETDATE(),
                        usuario_solicitud_id = @UserId,
                        updated_at           = GETDATE()
                    WHERE id    IN @Ids
                      AND status = 'SOLICITAR';";

                var affected = await _db.ExecuteAsync(sql, new
                {
                    Ids    = dto.SolicitudIds,
                    UserId = int.TryParse(userId, out var uid) ? uid : (int?)null
                });

                return Ok(new { success = true, registrosAfectados = affected, montoSeleccionado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PUT /pagos/autorizar-pagos
        //  PAGO SOLICITADO → AUTORIZADO  (acción del corporativo)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPut("autorizar-pagos")]
        public async Task<IActionResult> AutorizarPagos([FromBody] AutorizarPagosRequest dto)
        {
            try
            {
                if (dto.SolicitudIds == null || dto.SolicitudIds.Length == 0)
                    return BadRequest(new { message = "Debe seleccionar al menos una solicitud" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var sql = @"
                    UPDATE solicitudes_pago
                    SET status                  = 'AUTORIZADO',
                        fecha_autorizacion      = @FechaAutorizacion,
                        usuario_autorizacion_id = @UserId,
                        updated_at              = GETDATE()
                    WHERE id    IN @Ids
                      AND status = 'PAGO SOLICITADO';";

                var affected = await _db.ExecuteAsync(sql, new
                {
                    Ids               = dto.SolicitudIds,
                    dto.FechaAutorizacion,
                    UserId = int.TryParse(userId, out var uid) ? uid : (int?)null
                });

                return Ok(new { success = true, registrosAfectados = affected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PUT /pagos/registrar-pago
        //  AUTORIZADO → PAGADO  (sede o corporativo ejecuta el pago)
        // ═══════════════════════════════════════════════════════════════════
        [HttpPut("registrar-pago")]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoRequest dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var sql = @"
                    UPDATE solicitudes_pago
                    SET status           = 'PAGADO',
                        fecha_pago       = @FechaPago,
                        folio_pago       = @FolioPago,
                        banco_id         = ISNULL(@BancoId,     banco_id),
                        metodo_pago      = ISNULL(@FormaPagoId, metodo_pago),
                        monto_solicitado = CASE WHEN @ImportePago > 0
                                               THEN @ImportePago
                                               ELSE monto_solicitado END,
                        usuario_pago_id  = @UserId,
                        updated_at       = GETDATE()
                    WHERE id     = @SolicitudId
                      AND status = 'AUTORIZADO';";

                var affected = await _db.ExecuteAsync(sql, new
                {
                    dto.SolicitudId,
                    dto.FechaPago,
                    dto.FolioPago,
                    dto.BancoId,
                    dto.FormaPagoId,
                    dto.Cuenta,
                    dto.ImportePago,
                    UserId = int.TryParse(userId, out var uid) ? uid : (int?)null
                });

                if (affected == 0)
                    return BadRequest(new { message = "No se registró el pago. Verifique que el status sea AUTORIZADO." });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  GET /pagos/exportar-excel?sedeId=8&corporativo=false
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("exportar-excel")]
        public async Task<IActionResult> ExportarExcel([FromQuery] int sedeId, [FromQuery] bool corporativo = false)
        {
            try
            {
                var whereExtra = corporativo
                    ? "AND sp.status IN ('PAGO SOLICITADO','AUTORIZADO','PAGADO')"
                    : "";

                var sql = $@"
                    SELECT
                        b.ticket_numero                             AS Ticket,
                        FORMAT(fr.fecha_recepcion,'dd/MM/yyyy')    AS [Fecha Entrega],
                        fr.rfc_productor                           AS RFC,
                        p.nombre                                   AS Productor,
                        ISNULL(sc.nombre_sede, '')                 AS Sede,
                        fr.kg_total_entregados / 1000.0            AS Toneladas,
                        fr.precio_promedio                         AS Precio,
                        fr.importe_factura                         AS Importe,
                        sp.monto_solicitado                        AS [A Pagar],
                        sp.status                                  AS Status,
                        FORMAT(sp.fecha_solicitud,   'dd/MM/yyyy') AS [Fecha Solicitud],
                        FORMAT(sp.fecha_autorizacion,'dd/MM/yyyy') AS [Fecha Autorización],
                        FORMAT(sp.fecha_pago,        'dd/MM/yyyy') AS [Fecha Pago],
                        sp.folio_pago                              AS [Folio Pago],
                        bc.nombre_banco                            AS Banco,
                        cpf.nombre                                 AS [Forma Pago],
                        sp.cuenta_clabe                            AS CLABE
                    FROM solicitudes_pago sp
                    INNER JOIN facturacion_recepciones fr      ON sp.facturacion_id = fr.id
                    INNER JOIN boletas b                       ON fr.boleta_id       = b.id
                    INNER JOIN productores p                   ON fr.productor_id    = p.id
                    LEFT  JOIN sedes_catalogo sc               ON fr.sede_id         = sc.id
                    LEFT  JOIN bancos_catalogo bc              ON sp.banco_id        = bc.id
                    LEFT  JOIN Configuracion_Pagos_Formas cpf  ON sp.metodo_pago     = cpf.id
                    WHERE (@sedeId = 0 OR fr.sede_id = @sedeId)
                    {whereExtra}
                    ORDER BY sp.created_at DESC;";

                var rows = (await _db.QueryAsync<dynamic>(sql, new { sedeId })).ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Pagos");

                if (rows.Count > 0)
                {
                    var headers = ((IDictionary<string, object>)rows[0]).Keys.ToList();
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var cell = ws.Cell(1, c + 1);
                        cell.Value = headers[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    int row = 2;
                    foreach (var r in rows)
                    {
                        var vals = ((IDictionary<string, object>)r).Values.ToList();
                        for (int c = 0; c < vals.Count; c++)
                            ws.Cell(row, c + 1).Value = vals[c]?.ToString() ?? "";
                        row++;
                    }
                }

                ws.Columns().AdjustToContents();
                using var ms = new System.IO.MemoryStream();
                wb.SaveAs(ms);

                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"pagos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
