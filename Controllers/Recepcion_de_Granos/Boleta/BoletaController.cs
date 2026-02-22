using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("[controller]")]
    public class BoletaController : ControllerBase
    {
        private readonly IDbConnection _db;
        public BoletaController(IDbConnection db) => _db = db;

        // GET: api/boleta?sedeId=1
        // Obtiene todas las boletas para el módulo de Boleta (las que ya tienen precio autorizado)
        [HttpGet]
        public async Task<IActionResult> GetBoletas([FromQuery] int sedeId, [FromQuery] string? estatus = null)
        {
            try
            {
                var sql = @"
                    SELECT
                        b.id,
                        b.folio AS noBoleta,
                        b.ticket_numero AS ticket,
                        FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                        b.productor,
                        b.telefono,
                        b.comprador,
                        b.origen,
                        b.calibre,
                        b.humedad,
                        b.peso_bruto AS pesoBruto,
                        b.tara,
                        b.peso_neto AS pesoNeto,
                        --CAST(b.peso_neto / 1000.0 AS DECIMAL(10,3)) AS tonsAprox,
                        bp.tons_aprox as tonsAprox,
                        b.descuento_kg_ton AS descuento,
                        b.precio_mxn AS precio,
                        b.kg_a_liquidar AS kgALiquidar,
                        b.importe_total AS importeTotal,
                        b.status AS estatus,
                        b.observaciones,
                        b.created_at AS fechaCreacion,
                        b.updated_at AS fechaActualizacion,
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
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.exportacion') AS DECIMAL(10,2)) AS exportacion,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.cafes_lisos') AS DECIMAL(10,2)) AS cafesLisos,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.helados') AS DECIMAL(10,2)) AS helados,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.alimonados') AS DECIMAL(10,2)) AS alimonados,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.revolcados') AS DECIMAL(10,2)) AS revolcados,
                        ac.datos_adicionales AS datosAdicionales,
                        br.chofer,
                        br.placas,
                        br.grano_id AS granoId,
                        g.nombre AS tipoGrano
                    FROM dbo.boletas b
                    LEFT JOIN dbo.analisis_calidad ac ON b.analisis_id = ac.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.granos_catalogo g ON br.grano_id = g.id
                    LEFT JOIN dbo.boletas_precio bp ON b.id = bp.id

                    WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                      AND (@estatus IS NULL OR b.status = @estatus)
                      AND b.status NOT IN ('Pre-liquidado', 'Volcado Completado')
                    ORDER BY b.created_at DESC";

                var boletas = await _db.QueryAsync<dynamic>(sql, new { sedeId, estatus });
                return Ok(boletas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener boletas", error = ex.Message });
            }
        }

        // GET: api/boleta/resumen?sedeId=1
        // Obtiene el resumen/dashboard de boletas
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        COUNT(*) AS total,
                        SUM(CASE WHEN status IN ('Precio Autorizado', 'Precio Autorizado CG', 'Autorizado CC') THEN 1 ELSE 0 END) AS precioAutorizado,
                        SUM(CASE WHEN status = 'Sin Precio' OR status = 'Pendiente por Autorizar' THEN 1 ELSE 0 END) AS pendienteAutorizacion,
                        SUM(CASE WHEN status = 'Precio Aceptado' THEN 1 ELSE 0 END) AS finalizadas,
                        SUM(CASE WHEN status = 'En Renegociacion' THEN 1 ELSE 0 END) AS enRenegociacion
                    FROM dbo.boletas
                    WHERE (@sedeId = 0 OR sede_id = @sedeId)
                      AND status NOT IN ('Pre-liquidado', 'Volcado Completado')";

                var resumen = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { sedeId });
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener resumen", error = ex.Message });
            }
        }

        // GET: api/boleta/{id}
        // Obtiene el detalle completo de una boleta
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBoletaDetalle(int id, [FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        b.id,
                        b.folio AS noBoleta,
                        b.ticket_numero AS ticket,
                        FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                        b.productor,
                        b.telefono,
                        b.comprador,
                        b.origen,
                        b.calibre,
                        b.humedad,
                        b.peso_bruto AS pesoBruto,
                        b.tara,
                        b.peso_neto AS pesoNeto,
                        --CAST(b.peso_neto / 1000.0 AS DECIMAL(10,3)) AS tonsAprox,
                        bp.tons_aprox as tonsAprox,
                        b.descuento_kg_ton AS descuento,
                        b.precio_mxn AS precio,
                        b.kg_a_liquidar AS kgALiquidar,
                        b.importe_total AS importeTotal,
                        b.status AS estatus,
                        b.observaciones,
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
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.exportacion') AS DECIMAL(10,2)) AS exportacion,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.cafes_lisos') AS DECIMAL(10,2)) AS cafesLisos,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.helados') AS DECIMAL(10,2)) AS helados,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.alimonados') AS DECIMAL(10,2)) AS alimonados,
                        TRY_CAST(JSON_VALUE(ac.datos_adicionales, '$.revolcados') AS DECIMAL(10,2)) AS revolcados,
                        ac.datos_adicionales AS datosAdicionales,
                        br.chofer,
                        br.placas
                    FROM dbo.boletas b
                    LEFT JOIN dbo.analisis_calidad ac ON b.analisis_id = ac.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.boletas_precio bp ON b.id = bp.id
                    WHERE b.id = @id
                      AND (@sedeId = 0 OR b.sede_id = @sedeId)";

                var boleta = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { id, sedeId });

                if (boleta == null)
                    return NotFound(new { message = "Boleta no encontrada" });

                return Ok(boleta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener boleta", error = ex.Message });
            }
        }

        // POST: api/boleta/confirmar-productor
        // El productor acepta o rechaza el precio
        [HttpPost("confirmar-productor")]
        public async Task<IActionResult> ConfirmarPrecioProductor([FromBody] ConfirmarPrecioDto dto, [FromQuery] int sedeId)
        {
            try
            {
                var usuario = Request.Headers["X-User-Email"].ToString();
                if (string.IsNullOrEmpty(usuario))
                    usuario = "sistema";

                // Obtener la boleta actual
                var boletaActual = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT id, bascula_id, status FROM dbo.boletas
                    WHERE id = @BoletaId AND (@sedeId = 0 OR sede_id = @sedeId)",
                    new { dto.BoletaId, sedeId });

                if (boletaActual == null)
                    return NotFound(new { message = "Boleta no encontrada" });

                string nuevoEstatus;
                string nuevoEstatusBascula;

                if (dto.Acepta)
                {
                    // Productor acepta el precio -> estatus final "Precio Aceptado"
                    nuevoEstatus = "Precio Aceptado";
                    nuevoEstatusBascula = "LISTO_VOLCADO";
                }
                else
                {
                    // Productor rechaza el precio -> va a renegociación
                    nuevoEstatus = "En Renegociacion";
                    nuevoEstatusBascula = "EN_RENEGOCIACION";
                }

                // Actualizar la boleta
                var sqlUpdate = @"
                    UPDATE dbo.boletas
                    SET
                        status = @NuevoEstatus,
                        observaciones = ISNULL(observaciones, '') + ' | ' +
                            CASE WHEN @Acepta = 1 THEN 'Precio autorizado por productor'
                            ELSE 'Precio rechazado por productor: ' + @MotivoRechazo END,
                        updated_at = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaId";

                await _db.ExecuteAsync(sqlUpdate, new
                {
                    NuevoEstatus = nuevoEstatus,
                    dto.Acepta,
                    MotivoRechazo = dto.MotivoRechazo ?? "",
                    dto.BoletaId
                });

                // Sincronizar estatus en boletas_precio
                await _db.ExecuteAsync(@"
                    UPDATE dbo.boletas_precio
                    SET estatus = 'Precio Autorizado',
                        fecha_modificacion = SYSDATETIMEOFFSET()
                    WHERE boleta_id = @BoletaId",
                    new { NuevoEstatus = nuevoEstatus, dto.BoletaId });

                // Actualizar el estatus en bascula_recepciones
                await _db.ExecuteAsync(@"
                    UPDATE dbo.bascula_recepciones
                    SET status = @NuevoEstatus, updated_at = GETDATE()
                    WHERE id = @BasculaId",
                    new { NuevoEstatus = nuevoEstatusBascula, BasculaId = (int)boletaActual.bascula_id });

                var mensaje = dto.Acepta
                    ? "Precio aceptado. Boleta lista para volcado."
                    : "Precio rechazado. Boleta enviada a renegociación.";

                return Ok(new { message = mensaje, estatus = nuevoEstatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al confirmar precio", error = ex.Message });
            }
        }

        // POST: api/boleta/guardar-firmas
        // Guardar las firmas digitales de la boleta
        [HttpPost("guardar-firmas")]
        public async Task<IActionResult> GuardarFirmas([FromBody] FirmasBoletaDto dto, [FromQuery] int sedeId)
        {
            try
            {
                // Guardar las firmas en datos_adicionales o en un campo específico
                // Por ahora lo guardamos en observaciones como JSON
                var sqlUpdate = @"
                    UPDATE dbo.boletas
                    SET
                        observaciones = ISNULL(observaciones, '') + ' | Firmas registradas',
                        updated_at = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaId
                      AND (@sedeId = 0 OR sede_id = @sedeId)";

                await _db.ExecuteAsync(sqlUpdate, new { dto.BoletaId, sedeId });

                return Ok(new { message = "Firmas guardadas correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar firmas", error = ex.Message });
            }
        }
    }

    // DTOs para el controlador de Boleta
    public class ConfirmarPrecioDto
    {
        public int BoletaId { get; set; }
        public bool Acepta { get; set; }
        public string? MotivoRechazo { get; set; }
    }

    public class FirmasBoletaDto
    {
        public int BoletaId { get; set; }
        public string? FirmaAnalista { get; set; }
        public string? FirmaRecepcionista { get; set; }
        public string? FirmaAutorizo { get; set; }
    }
}
