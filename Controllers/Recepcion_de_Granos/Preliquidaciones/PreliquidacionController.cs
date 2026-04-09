using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Alazan.API.Controllers
{
    [Authorize] // Requiere token válido para todos los métodos
    [ApiController]
    [Route("[controller]")] // Ruta estándar
    public class PreliquidacionController : ControllerBase
    {
        private readonly IDbConnection _db;
        public PreliquidacionController(IDbConnection db) => _db = db;

        // GET: api/preliquidacion?sedeId=1
        [HttpGet]
        public async Task<IActionResult> GetRegistros([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        ROW_NUMBER() OVER (ORDER BY b.created_at ASC) AS numero,
                        b.id AS boletaId,
                        b.ticket_numero AS ticket,
                        b.folio AS noBoleta,
                        FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                        b.productor,
                        b.telefono,
                        b.comprador,
                        b.origen,
                        b.peso_bruto AS pesoBruto,
                        p.tara_kg AS pesoTara,
                        p.peso_neto_kg AS pesoNeto,
                        CAST(p.peso_neto_kg / 1000.0 AS DECIMAL(10,3)) AS tonsAprox,
                        b.precio_mxn AS precio,
                        ISNULL(p.descuento_kg_ton, b.descuento_kg_ton) AS descuento,
                        ISNULL(p.kg_a_liquidar, b.kg_a_liquidar) AS kgALiquidar,
                        ISNULL(p.importe_total, b.importe_total) AS importeTotal,
                        CASE
                            WHEN p.id IS NOT NULL THEN 'Con Preliquidacion'
                            ELSE 'Sin Preliquidacion'
                        END AS status,
                        br.chofer,
                        br.placas,
                        JSON_VALUE(br.datos_adicionales, '$.tipo_productor') AS tProductor,
                        g.nombre AS grano,
                        br.grano_id AS granoId,
                        v.bodega_ubicacion AS siloNombre,
                        ac.impurezas,
                        ac.datos_adicionales AS datosAdicionales,
                        p.id AS preliquidacionId
                    FROM dbo.boletas b
                    INNER JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.granos_catalogo g ON br.grano_id = g.id
                    LEFT JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                    LEFT JOIN dbo.analisis_calidad ac ON b.analisis_id = ac.id
                    LEFT JOIN dbo.preliquidaciones p ON p.boleta_id = b.id
                    WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                      AND v.status IN ('Con Silo Asignado', 'Con Almacen Asignado')
                    ORDER BY b.created_at DESC";

                var registros = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(registros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener registros de pre-liquidación", error = ex.Message });
            }
        }

        // GET: api/preliquidacion/resumen?sedeId=1
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        COUNT(*) AS totalDelDia,
                        SUM(CASE WHEN p.id IS NOT NULL THEN 1 ELSE 0 END) AS conPreliquidacion,
                        SUM(CASE WHEN p.id IS NULL THEN 1 ELSE 0 END) AS sinPreliquidacion,
                        CAST(SUM(ISNULL(p.peso_neto_kg, 0)) / 1000.0 AS DECIMAL(10,2)) AS totalToneladas
                    FROM dbo.boletas b
                    INNER JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                    LEFT JOIN dbo.preliquidaciones p ON p.boleta_id = b.id
                    WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                    AND v.status IN ('Con Silo Asignado', 'Con Almacen Asignado')";

                var resumen = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { sedeId });
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener resumen", error = ex.Message });
            }
        }

        // GET: api/preliquidacion/detalle/{boletaId}?sedeId=1
        [HttpGet("detalle/{boletaId}")]
        public async Task<IActionResult> GetDetalle(int boletaId, [FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                    b.id AS boletaId,
                    b.folio AS noBoleta,
                    b.ticket_numero AS ticket,
                    FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                    b.productor,
                    b.telefono,
                    b.comprador,
                    b.origen,
                    b.calibre,
                    prod.rfc,
                    prod.atiende,
                    ISNULL(prod.nombre, b.productor) AS razonSocial,
                    b.humedad,
                    b.peso_bruto AS pesoBruto,
                    p.tara_kg AS pesoTara,
                    p.peso_neto_kg AS pesoNeto,
                    bp.tons_aprox as tonsAprox,
                    b.precio_mxn AS precio,
                    ISNULL(p.descuento_kg_ton, b.descuento_kg_ton) AS descuento,
                    ISNULL(p.kg_a_liquidar, b.kg_a_liquidar) AS kgALiquidar,
                    ISNULL(p.importe_total, b.importe_total) AS importeTotal,
                    
                    -- Observaciones: si ya hay preliquidación guardada usar la guardada, si no usar plantilla según tipo productor
                    CASE
                        WHEN p.id IS NOT NULL THEN p.observaciones
                        WHEN JSON_VALUE(br.datos_adicionales, '$.tipo_productor') = 'Ejidal'            THEN CAST(crr.tpl_ejidal AS NVARCHAR(MAX))
                        WHEN JSON_VALUE(br.datos_adicionales, '$.tipo_productor') = 'Pequeña Propiedad' THEN CAST(crr.tpl_pequena_propiedad AS NVARCHAR(MAX))
                        WHEN JSON_VALUE(br.datos_adicionales, '$.tipo_productor') = 'Persona Moral'     THEN CAST(crr.tpl_persona_moral AS NVARCHAR(MAX))
                        ELSE CAST(crr.tpl_ejidal AS NVARCHAR(MAX))
                    END AS observaciones,

                    b.status,
                    br.chofer,
                    br.placas,
                    b.datos_adicionales AS datosAdicionales,
                    JSON_VALUE(br.datos_adicionales, '$.tipo_productor') AS tProductor,
                    g.nombre AS grano,
                    br.grano_id AS granoId,
                    ac.datos_adicionales AS analisisDatosAdicionales,
                    JSON_VALUE(ac.datos_adicionales, '$.fotos[0]') AS foto,
                    ac.impurezas,
                    ac.r1_danado_insecto AS r1,
                    (ISNULL(ac.r2_quebrado,0) + ISNULL(ac.r2_manchado,0) + ISNULL(ac.r2_arrugado,0) +
                     ISNULL(TRY_CAST(JSON_VALUE(ac.datos_adicionales,'$.cafes_lisos') AS DECIMAL(10,2)),0) +
                     ISNULL(TRY_CAST(JSON_VALUE(ac.datos_adicionales,'$.helados') AS DECIMAL(10,2)),0) +
                     ISNULL(TRY_CAST(JSON_VALUE(ac.datos_adicionales,'$.alimonados') AS DECIMAL(10,2)),0) +
                     ISNULL(TRY_CAST(JSON_VALUE(ac.datos_adicionales,'$.revolcados') AS DECIMAL(10,2)),0)) AS sumaR2,
                    ac.r2_arrugado AS r2,
                    ac.r2_manchado AS manchados,
                    ac.r2_quebrado AS quebMxc,
                    TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.cafes_lisos') AS DECIMAL(10,2)) AS cafesLisos,
                    TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.helados') AS DECIMAL(10,2)) AS helados,
                    TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.alimonados') AS DECIMAL(10,2)) AS alimonados,
                    TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.revolcados') AS DECIMAL(10,2)) AS revolcados,
                    TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.exportacion') AS DECIMAL(10,2)) AS exportacion,
                    v.bodega_ubicacion AS siloNombre,
                    p.id AS preliquidacionId,
                    p.tipo_siembra AS tipoSiembra,
                    br.productor_id AS productorId,
                    p.divisiones_json AS divisionesJson
                FROM dbo.boletas b
                LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                LEFT JOIN dbo.granos_catalogo g ON br.grano_id = g.id
                LEFT JOIN dbo.analisis_calidad ac ON b.analisis_id = ac.id
                LEFT JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                LEFT JOIN dbo.preliquidaciones p ON p.boleta_id = b.id
                LEFT JOIN dbo.boletas_precio bp ON b.id = bp.id
                LEFT JOIN dbo.productores prod ON br.productor_id = prod.id
                -- Unión para obtener las reglas de la sede
                LEFT JOIN dbo.CONFIGURACION_RECEPCION_REGLAS crr ON b.sede_id = crr.sede_id
                WHERE b.id = @boletaId
                AND (@sedeId = 0 OR b.sede_id = @sedeId)";

                var detalle = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { boletaId, sedeId });

                if (detalle == null) return NotFound(new { message = "Boleta no encontrada" });

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener detalle", error = ex.Message });
            }
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarPreliquidacion([FromBody] PreliquidacionDto dto)
        {
            try
            {
                // Obtener ID de usuario desde el JWT para auditoría
                long? usuarioId = null;
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(usuarioIdClaim, out var uid))
                    usuarioId = uid;

                // Serializar divisiones si existen
                string? divisionesJson = null;
                if (dto.Divisiones != null && dto.Divisiones.Count > 1)
                {
                    divisionesJson = JsonSerializer.Serialize(new
                    {
                        total_productores = dto.Divisiones.Count,
                        productores = dto.Divisiones
                    });
                }

                // 1. Insertar en la tabla preliquidaciones
                var sqlPreliq = @"
                    INSERT INTO dbo.preliquidaciones
                        (ticket_numero, bascula_id, boleta_id, sede_id,
                         productor_id,
                         kg_neto_recibido, calibre, precio_base_mxn_ton,
                         descuento_kg_ton, kg_a_liquidar, precio_final_mxn_ton,
                         importe_total, tara_kg, peso_neto_kg, tipo_siembra,
                         observaciones, status, divisiones_json)
                    SELECT
                        b.ticket_numero,
                        b.bascula_id,
                        b.id,
                        b.sede_id,
                        br.productor_id,
                        @PesoNeto,
                        b.calibre,
                        b.precio_mxn,
                        @Descuento,
                        @KgALiquidar,
                        b.precio_mxn,
                        @ImporteTotal,
                        @PesoTara,
                        @PesoNeto,
                        @Rt,
                        @Observaciones,
                        'PENDIENTE',
                        @DivisionesJson
                    FROM dbo.boletas b
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    WHERE b.id = @BoletaId
                      AND NOT EXISTS (SELECT 1 FROM dbo.preliquidaciones WHERE boleta_id = @BoletaId)";

                await _db.ExecuteAsync(sqlPreliq, new
                {
                    dto.PesoNeto, dto.Descuento, dto.KgALiquidar, dto.ImporteTotal,
                    dto.PesoTara, dto.Rt, dto.Observaciones, dto.BoletaId,
                    DivisionesJson = divisionesJson
                });

                // 2. Actualizar peso_neto_kg y tara_kg en bascula_recepciones
                var sqlBascula = @"
                    UPDATE dbo.bascula_recepciones
                    SET peso_neto_kg = @PesoNeto,
                        status = 'FINALIZADO',
                        tara_kg = @PesoTara,
                        updated_at = GETDATE()
                    WHERE id = (SELECT bascula_id FROM dbo.boletas WHERE id = @BoletaId)";

                await _db.ExecuteAsync(sqlBascula, dto);

                // 3a. Actualizar kg_neto en inventario_silos con el peso real (bruto - tara)
                await _db.ExecuteAsync(@"
                    UPDATE dbo.inventario_silos
                    SET kg_neto   = @PesoNeto,
                        updated_at = SYSDATETIMEOFFSET()
                    WHERE bascula_id = (SELECT bascula_id FROM dbo.boletas WHERE id = @BoletaId)",
                    new { dto.PesoNeto, dto.BoletaId });

                // 4. Crear registros en facturacion_recepciones
                if (dto.Divisiones != null && dto.Divisiones.Count > 1)
                {
                    // Obtener el id de la preliquidación recién creada
                    var preliqId = await _db.ExecuteScalarAsync<int>(
                        "SELECT TOP 1 id FROM dbo.preliquidaciones WHERE boleta_id = @BoletaId ORDER BY id DESC",
                        new { dto.BoletaId });

                    var boleta = await _db.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT sede_id, precio_mxn, ticket_numero FROM dbo.boletas WHERE id = @BoletaId",
                        new { dto.BoletaId });

                    var sqlFactDiv = @"
                        INSERT INTO dbo.facturacion_recepciones
                            (productor_id, preliquidacion_id, boleta_id, monto_total, status,
                             fecha_recepcion, rfc_productor, importe_factura,
                             kg_total_entregados, precio_promedio,
                             tiene_documentos, tiene_factura_xml,
                             created_at, updated_at, sede_id, ticket_numero, usuario_registro_id)
                        SELECT
                            @ProductorId,
                            @PreliqId,
                            @BoletaId,
                            @ImporteTotal,
                            'PENDIENTE',
                            GETDATE(),
                            NULLIF(p.rfc, ''),
                            @ImporteTotal,
                            @KgAsignados,
                            @PrecioMxn,
                            0, 0,
                            GETDATE(), GETDATE(),
                            @SedeId,
                            @TicketNumero,
                            @UsuarioId
                        FROM dbo.productores p
                        WHERE p.id = @ProductorId";

                    int orden = 1;
                    foreach (var div in dto.Divisiones)
                    {
                        await _db.ExecuteAsync(sqlFactDiv, new
                        {
                            div.ProductorId,
                            PreliqId = preliqId,
                            dto.BoletaId,
                            div.ImporteTotal,
                            div.KgAsignados,
                            PrecioMxn = boleta?.precio_mxn,
                            SedeId = boleta?.sede_id,
                            TicketNumero = $"{boleta?.ticket_numero}-P{orden}",
                            UsuarioId = usuarioId
                        });
                        orden++;
                    }
                }
                else
                {
                    // Flujo normal: un solo registro en facturacion_recepciones
                    var sqlFacturacion = @"
                        INSERT INTO dbo.facturacion_recepciones
                            (productor_id, preliquidacion_id, boleta_id, monto_total, status,
                             fecha_recepcion, rfc_productor, importe_factura,
                             kg_total_entregados, precio_promedio,
                             tiene_documentos, tiene_factura_xml,
                             created_at, updated_at, sede_id, ticket_numero, usuario_registro_id)
                        SELECT
                            br.productor_id,
                            (SELECT TOP 1 id FROM dbo.preliquidaciones WHERE boleta_id = @BoletaId ORDER BY id DESC),
                            @BoletaId,
                            @ImporteTotal,
                            'PENDIENTE',
                            GETDATE(),
                            NULLIF(p.rfc, ''),
                            @ImporteTotal,
                            @KgALiquidar,
                            b.precio_mxn,
                            0, 0,
                            GETDATE(), GETDATE(),
                            b.sede_id,
                            CAST(b.ticket_numero AS VARCHAR(50)),
                            @UsuarioId
                        FROM dbo.boletas b
                        LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                        LEFT JOIN dbo.productores p ON br.productor_id = p.id
                        WHERE b.id = @BoletaId
                          AND NOT EXISTS (SELECT 1 FROM dbo.facturacion_recepciones WHERE boleta_id = @BoletaId)";

                    await _db.ExecuteAsync(sqlFacturacion, new
                    {
                        dto.BoletaId,
                        dto.ImporteTotal,
                        dto.KgALiquidar,
                        UsuarioId = usuarioId
                    });
                }

                return Ok(new { message = "Pre-liquidación guardada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar pre-liquidación", error = ex.Message });
            }
        }

        public class FotoDocumentoRequest
        {
            public int BoletaId { get; set; }
            public string Foto { get; set; }
        }

        [HttpPost("guardar-foto")]
        public async Task<IActionResult> GuardarFoto([FromBody] FotoDocumentoRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Foto))
                    return BadRequest(new { message = "Datos de imagen no recibidos." });

                // Actualizamos la columna datos_adicionales
                const string sql = @"
                    UPDATE dbo.boletas
                    SET datos_adicionales = @Foto,
                        updated_at = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaId";

                await _db.ExecuteAsync(sql, new {
                    Foto = request.Foto,
                    BoletaId = request.BoletaId
                });

                return Ok(new { message = "Documentación guardada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en servidor: " + ex.Message });
            }
        }
    }

    public class PreliquidacionDto
    {
        public int BoletaId { get; set; }
        public decimal? PesoTara { get; set; }
        public decimal? PesoNeto { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? KgALiquidar { get; set; }
        public decimal? ImporteTotal { get; set; }
        public string? Observaciones { get; set; }
        public string? Rt { get; set; }
        public List<ProductorDivisionDto>? Divisiones { get; set; }
    }

    public class ProductorDivisionDto
    {
        public int ProductorId { get; set; }
        public string Nombre { get; set; } = "";
        public decimal KgAsignados { get; set; }
        public decimal ImporteTotal { get; set; }
    }
}
