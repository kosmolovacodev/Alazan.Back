using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Alazan.API.Models;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("[controller]")]
    public class PrecioController : ControllerBase
    {
        private readonly IDbConnection _db;
        public PrecioController(IDbConnection db) => _db = db;

        // GET: api/precio?sedeId=1
        [HttpGet]
        public async Task<IActionResult> GetBoletasPrecios([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        bp.id,
                        bp.no_boleta AS noBoleta,
                        bp.ticket,
                        FORMAT(bp.fecha_registro, 'yyyy-MM-dd HH:mm') AS fecha,
                        bp.productor_nombre AS productor,
                        bp.telefono,
                        bp.comprador,
                        bp.origen,
                        bp.calibre,
                        bp.peso_bruto AS pesoBruto,
                        b.peso_neto,
                        bp.tons_aprox AS tonsAprox,
                        bp.descuento_kg_ton AS descuento,
                        bp.precio_sugerido AS precioSugerido,
                        bp.precio_sugerido_codigo AS precioSugeridoCodigo,
                        bp.precio_autorizado AS precioAutorizado,
                        bp.precio_final AS precioFinal,
                        b.kg_a_liquidar,
                        b.importe_total,
                        bp.estatus,
                        bp.tiempo_registro AS tiempoRegistro,
                        bp.es_de_analisis AS esDeAnalisis,
                        bp.autorizacion_automatica AS autorizacionAutomatica,
                        -- Datos de analisis de calidad
                        ac.humedad,
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
                        b.t_productor AS tProductor,
                        bp.tipo_grano AS tipoGrano,
                        br.grano_id AS granoId
                    FROM dbo.boletas_precio bp
                    LEFT JOIN dbo.boletas b ON bp.boleta_id = b.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.analisis_calidad ac ON b.analisis_id = ac.id
                    WHERE (@sedeId = 0 OR bp.sede_id = @sedeId)
                      --AND bp.estatus IN ('Sin Precio', 'Pendiente por Autorizar', 'Precio Autorizado', 'Precio Autorizado CG', 'Autorizado CC', 'Pendiente por Renegociar', 'Precio Rechazado')
                      AND bp.estatus IN ('Sin Precio','Precio Autorizado','En Renegociacion','Precio Rechazado')
                    ORDER BY bp.fecha_registro ASC";

                var boletas = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(boletas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener boletas", error = ex.Message });
            }
        }

        // POST: api/precio/autorizar
        [HttpPost("autorizar")]
        public async Task<IActionResult> AutorizarPrecio([FromBody] AutorizarPrecioDto dto, [FromQuery] int sedeId)
        {
            try
            {
                var usuario = Request.Headers["X-User-Email"].ToString();
                if (string.IsNullOrEmpty(usuario))
                    usuario = "sistema";

                // Obtener la boleta_precio y su boleta_id
                var boletaPrecio = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT bp.id, bp.boleta_id, b.bascula_id
                    FROM dbo.boletas_precio bp
                    LEFT JOIN dbo.boletas b ON bp.boleta_id = b.id
                    WHERE bp.id = @BoletaPrecioId AND (@sedeId = 0 OR bp.sede_id = @sedeId)",
                    new { dto.BoletaPrecioId, sedeId });

                if (boletaPrecio == null)
                    return NotFound(new { message = "Boleta de precio no encontrada" });

                // Determinar estatus segun tipo de autorizacion
                string nuevoEstatus;
                switch (dto.TipoAutorizacion?.ToUpper())
                {
                    case "CC":
                        //nuevoEstatus = "Autorizado CC";
                        nuevoEstatus = "Precio Autorizado";
                        break;
                    case "CG":
                        //nuevoEstatus = "Precio Autorizado CG";
                        nuevoEstatus = "Precio Autorizado";
                        break;
                    default:
                        nuevoEstatus = "Precio Autorizado";
                        break;
                }

                // 1. Actualizar boletas_precio
                await _db.ExecuteAsync(@"
                    UPDATE dbo.boletas_precio
                    SET estatus = @NuevoEstatus,
                        precio_autorizado = @PrecioAutorizado,
                        precio_final = @PrecioAutorizado,
                        tiempo_autorizacion = SYSDATETIMEOFFSET(),
                        usuario_autorizacion = @Usuario,
                        autorizacion_automatica = @EsAutomatica,
                        observaciones = ISNULL(observaciones, '') + ' | Autorizado (' + @TipoAuth + '): ' + @Observaciones,
                        fecha_modificacion = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaPrecioId",
                    new
                    {
                        NuevoEstatus = nuevoEstatus,
                        dto.PrecioAutorizado,
                        Usuario = usuario,
                        EsAutomatica = dto.AutorizacionAutomatica,
                        TipoAuth = dto.TipoAutorizacion ?? "NORMAL",
                        Observaciones = dto.Observaciones ?? "",
                        dto.BoletaPrecioId
                    });

                // 2. Sincronizar estatus en boletas (para el flujo Boleta/Volcado/Preliquidacion)
                if (boletaPrecio.boleta_id != null)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.boletas
                        SET status = @NuevoEstatus,
                            precio_mxn = @PrecioAutorizado,
                            peso_neto = 0,
                            observaciones = ISNULL(observaciones, '') + ' | Autorizado (' + @TipoAuth + '): ' + @Observaciones,
                            updated_at = SYSDATETIMEOFFSET()
                        WHERE id = @BoletaId",
                        new
                        {
                            NuevoEstatus = nuevoEstatus,
                            dto.PrecioAutorizado,
                            TipoAuth = dto.TipoAutorizacion ?? "NORMAL",
                            Observaciones = dto.Observaciones ?? "",
                            BoletaId = (int)boletaPrecio.boleta_id
                        });
                }

                // 3. Actualizar bascula_recepciones
                if (boletaPrecio.bascula_id != null)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.bascula_recepciones
                        SET status = 'AUTORIZADO',
                            updated_at = GETDATE()
                        WHERE id = @BasculaId",
                        new { BasculaId = (int)boletaPrecio.bascula_id });
                }

                return Ok(new { message = "Precio autorizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al autorizar precio", error = ex.Message });
            }
        }

        // POST: api/precio/renegociar
        [HttpPost("renegociar")]
        public async Task<IActionResult> RenegociarPrecio([FromBody] RenegociarPrecioDto dto, [FromQuery] int sedeId)
        {
            try
            {
                var usuario = Request.Headers["X-User-Email"].ToString();
                if (string.IsNullOrEmpty(usuario))
                    usuario = "sistema";

                // Obtener boleta_precio
                var boletaPrecio = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT bp.id, bp.boleta_id, b.bascula_id
                    FROM dbo.boletas_precio bp
                    LEFT JOIN dbo.boletas b ON bp.boleta_id = b.id
                    WHERE bp.id = @BoletaPrecioId AND (@sedeId = 0 OR bp.sede_id = @sedeId)",
                    new { dto.BoletaPrecioId, sedeId });

                if (boletaPrecio == null)
                    return NotFound(new { message = "Boleta de precio no encontrada" });

                // 1. Actualizar boletas_precio
                await _db.ExecuteAsync(@"
                    UPDATE dbo.boletas_precio
                    SET precio_autorizado = @PrecioNuevo,
                        precio_final = @PrecioNuevo,
                        estatus = 'Precio Autorizado',
                        tiempo_autorizacion = SYSDATETIMEOFFSET(),
                        usuario_autorizacion = @Usuario,
                        observaciones = ISNULL(observaciones, '') + ' | Renegociado: ' + @MotivoRenegociacion,
                        fecha_modificacion = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaPrecioId",
                    new
                    {
                        dto.PrecioNuevo,
                        Usuario = usuario,
                        MotivoRenegociacion = dto.MotivoRenegociacion ?? "",
                        dto.BoletaPrecioId
                    });

                // 2. Sincronizar en boletas
                if (boletaPrecio.boleta_id != null)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.boletas
                        SET precio_mxn = @PrecioNuevo,
                            status = 'Precio Autorizado',
                            observaciones = ISNULL(observaciones, '') + ' | Renegociado: ' + @MotivoRenegociacion,
                            updated_at = SYSDATETIMEOFFSET()
                        WHERE id = @BoletaId",
                        new
                        {
                            dto.PrecioNuevo,
                            MotivoRenegociacion = dto.MotivoRenegociacion ?? "",
                            BoletaId = (int)boletaPrecio.boleta_id
                        });
                }

                // 3. Actualizar bascula_recepciones
                if (boletaPrecio.bascula_id != null)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.bascula_recepciones
                        SET status = 'AUTORIZADO', updated_at = GETDATE()
                        WHERE id = @BasculaId",
                        new { BasculaId = (int)boletaPrecio.bascula_id });
                }

                return Ok(new { message = "Precio renegociado y autorizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al renegociar precio", error = ex.Message });
            }
        }

        // POST: api/precio/rechazar
        [HttpPost("rechazar")]
        public async Task<IActionResult> RechazarPrecio([FromBody] RechazarPrecioDto dto, [FromQuery] int sedeId)
        {
            try
            {
                var usuario = Request.Headers["X-User-Email"].ToString();
                if (string.IsNullOrEmpty(usuario))
                    usuario = "sistema";

                // Obtener boleta_precio
                var boletaPrecio = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT bp.id, bp.boleta_id
                    FROM dbo.boletas_precio bp
                    WHERE bp.id = @BoletaPrecioId AND (@sedeId = 0 OR bp.sede_id = @sedeId)",
                    new { dto.BoletaPrecioId, sedeId });

                if (boletaPrecio == null)
                    return NotFound(new { message = "Boleta de precio no encontrada" });

                // 1. Actualizar boletas_precio con estatus 'Precio Rechazado'
                await _db.ExecuteAsync(@"
                    UPDATE dbo.boletas_precio
                    SET estatus = 'Precio Rechazado',
                        observaciones = ISNULL(observaciones, '') + ' | Rechazado: ' + @MotivoRechazo,
                        fecha_modificacion = SYSDATETIMEOFFSET()
                    WHERE id = @BoletaPrecioId",
                    new
                    {
                        MotivoRechazo = dto.MotivoRechazo ?? "",
                        dto.BoletaPrecioId
                    });

                await _db.ExecuteAsync(@"
                    UPDATE dbo.bascula_recepciones
                    SET status = 'BOLETA RECHAZADA', updated_at = GETDATE()
                    WHERE id = (SELECT bascula_id FROM dbo.boletas WHERE id = @BoletaId)",
                    new { BoletaId = (int)boletaPrecio.boleta_id });

                // 2. Sincronizar en boletas con estatus 'Precio Rechazado' (para módulo Boleta)
                await _db.ExecuteAsync(@"
                        UPDATE dbo.boletas
                        SET status = 'Precio Rechazado',
                            observaciones = ISNULL(observaciones, '') + ' | Rechazado: ' + @MotivoRechazo,
                            updated_at = SYSDATETIMEOFFSET()
                        WHERE id = @BoletaId",
                        new
                        {
                            MotivoRechazo = dto.MotivoRechazo ?? "",
                            BoletaId = (int)boletaPrecio.boleta_id
                        });
                return Ok(new { message = "Precio rechazado. Boleta enviada a renegociacion." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al rechazar precio", error = ex.Message });
            }
        }

        // GET: api/precio/historial/{boletaPrecioId}
        [HttpGet("historial/{boletaPrecioId}")]
        public async Task<IActionResult> GetHistorialPrecio(int boletaPrecioId)
        {
            try
            {
                var sql = @"
                    SELECT
                        id AS Id,
                        no_boleta AS NoBoleta,
                        precio_anterior AS PrecioAnterior,
                        precio_nuevo AS PrecioNuevo,
                        motivo_cambio AS MotivoCambio,
                        tipo_accion AS TipoAccion,
                        usuario AS Usuario,
                        fecha_cambio AS FechaCambio
                    FROM dbo.Historial_Precio
                    WHERE boleta_precio_id = @BoletaPrecioId
                    ORDER BY fecha_cambio DESC";

                var historial = await _db.QueryAsync<HistorialPrecioDto>(sql, new { BoletaPrecioId = boletaPrecioId });
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener historial", error = ex.Message });
            }
        }

        // GET: api/precio/configuracion?sedeId=1
        [HttpGet("configuracion")]
        public async Task<IActionResult> GetConfiguracion([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        id AS Id,
                        sede_id AS SedeId,
                        habilitar_autorizacion_automatica AS HabilitarAutorizacionAutomatica,
                        minutos_para_autorizacion AS MinutosParaAutorizacion,
                        tolerancia_precio_pct AS ToleranciaPrecioPct,
                        requiere_autorizacion_fuera_tolerancia AS RequiereAutorizacionFueraTolerancia
                    FROM dbo.Configuracion_Precio
                    WHERE sede_id = @SedeId";

                var config = await _db.QueryFirstOrDefaultAsync<ConfiguracionPrecioDto>(sql, new { SedeId = sedeId });

                if (config == null)
                {
                    config = new ConfiguracionPrecioDto
                    {
                        SedeId = sedeId,
                        HabilitarAutorizacionAutomatica = false,
                        MinutosParaAutorizacion = 30,
                        ToleranciaPrecioPct = 5.0m,
                        RequiereAutorizacionFueraTolerancia = true
                    };
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener configuracion", error = ex.Message });
            }
        }
    }
}
