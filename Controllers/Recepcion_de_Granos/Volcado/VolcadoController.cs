using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VolcadoController : ControllerBase
    {
        private readonly IDbConnection _db;
        public VolcadoController(IDbConnection db) => _db = db;

        // GET: api/volcado?sedeId=1
        [HttpGet]
        public async Task<IActionResult> GetRegistrosVolcado([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        ROW_NUMBER() OVER (ORDER BY b.created_at DESC) AS numero,
                        b.id AS boletaId,
                        b.ticket_numero AS ticket,
                        b.folio AS noBoleta,
                        FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                        --CAST(b.peso_neto / 1000.0 AS DECIMAL(10,3)) AS tonsAprox,
                        bp.tons_aprox as tonsAprox,
                        br.chofer,
                        br.placas,
                        v.id AS volcadoId,
                        v.bodega_ubicacion AS siloNombre,
                        v.silo_numero AS siloNumero,
                        v.kg_volcados AS kgVolcados,
                        CASE
                            WHEN v.id IS NOT NULL AND v.status = 'Rechazado' THEN 'Rechazado'
                            WHEN v.id IS NOT NULL AND v.bodega_ubicacion IS NOT NULL THEN 'Con Silo Asignado'
                            ELSE 'Sin Silo Asignado'
                        END AS estatusSilo,
                        br.grano_id AS granoId,
                        g.nombre AS tipoGrano
                    FROM dbo.boletas b
                    INNER JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.granos_catalogo g ON br.grano_id = g.id
                    LEFT JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                    LEFT JOIN dbo.boletas_precio bp ON b.id = bp.id
                    WHERE b.sede_id = @sedeId
                    AND b.status IN ('Precio Aceptado', 'Volcado Completado', 'Pre-liquidado','En Renegociacion','Rechazado')";
                var registros = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(registros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener registros de volcado", error = ex.Message });
            }
        }

        // GET: api/volcado/resumen?sedeId=1
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        COUNT(*) AS totalDelDia,
                        SUM(CASE WHEN v.id IS NOT NULL AND v.bodega_ubicacion IS NOT NULL THEN 1 ELSE 0 END) AS conSiloAsignado,
                        SUM(CASE WHEN v.id IS NULL OR v.bodega_ubicacion IS NULL THEN 1 ELSE 0 END) AS sinSiloAsignado,
                        CAST(SUM(ISNULL(b.peso_neto, 0)) / 1000.0 AS DECIMAL(10,2)) AS totalToneladas
                    FROM dbo.boletas b
                    LEFT JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                    WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                      AND b.status IN ('Precio Aceptado', 'Volcado Completado', 'Pre-liquidado')";

                var resumen = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { sedeId });
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener resumen de volcado", error = ex.Message });
            }
        }

        // GET: api/volcado/silos
        [HttpGet("silos")]
        public async Task<IActionResult> GetSilos([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        id,
                        nombre_bodega AS nombreBodega,
                        numero_silo AS numeroSilo,
                        capacidad_ton AS capacidadTon,
                        activo
                    FROM dbo.bodegas_silos_catalogo
                    WHERE activo = 1
                    ORDER BY nombre_bodega, numero_silo";

                var silos = await _db.QueryAsync<dynamic>(sql);
                return Ok(silos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener silos", error = ex.Message });
            }
        }

        // GET: api/volcado/silos-calibre
        [HttpGet("silos-calibre")]
        public async Task<IActionResult> GetSilosCalibre([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        id,
                        nombre,
                        calibre_id,
                        capacidad_toneladas,
                        descripcion,
                        activo,
                        sede_id
                    FROM dbo.silos_calibre_catalogo
                    WHERE activo = 1
                      AND (@sedeId = 0 OR sede_id = @sedeId)
                    ORDER BY nombre";

                var silos = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(silos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener silos por calibre", error = ex.Message });
            }
        }

        // GET: api/volcado/silos-pulmon
        [HttpGet("silos-pulmon")]
        public async Task<IActionResult> GetSilosPulmon([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        id,
                        nombre,
                        capacidad_toneladas,
                        descripcion,
                        tipo,
                        activo,
                        sede_id
                    FROM dbo.silos_pulmon_catalogo
                    WHERE activo = 1
                      AND (@sedeId = 0 OR sede_id = @sedeId)
                    ORDER BY tipo, nombre";

                var silos = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                return Ok(silos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener silos pulmón", error = ex.Message });
            }
        }

        // GET: api/volcado/detalle/{boletaId}
        [HttpGet("detalle/{boletaId}")]
        public async Task<IActionResult> GetDetalleBoleta(int boletaId, [FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        b.folio AS noBoleta,
                        b.productor,
                        FORMAT(b.fecha_hora, 'yyyy-MM-dd HH:mm') AS fecha,
                        b.telefono,
                        b.comprador,
                        b.origen,
                        b.precio_mxn AS precio,
                        b.descuento_kg_ton AS descuento,
                        b.calibre,
                        b.humedad,
                        b.peso_bruto AS pesoBruto,
                        bp.tons_aprox as tonsAprox,
                        a.impurezas,
                        a.r1_danado_insecto AS r1,
                        (ISNULL(a.r2_quebrado,0) + ISNULL(a.r2_manchado,0) + ISNULL(a.r2_arrugado,0) +
                         ISNULL(TRY_CAST(JSON_VALUE(a.datos_adicionales,'$.cafes_lisos') AS DECIMAL(10,2)),0) +
                         ISNULL(TRY_CAST(JSON_VALUE(a.datos_adicionales,'$.helados') AS DECIMAL(10,2)),0) +
                         ISNULL(TRY_CAST(JSON_VALUE(a.datos_adicionales,'$.alimonados') AS DECIMAL(10,2)),0) +
                         ISNULL(TRY_CAST(JSON_VALUE(a.datos_adicionales,'$.revolcados') AS DECIMAL(10,2)),0)) AS sumaR2,
                        a.r2_arrugado AS r2,
                        a.r2_manchado AS manchados,
                        a.r2_quebrado AS quebMxc,
                        TRY_CAST(JSON_VALUE(a.datos_adicionales, '$.cafes_lisos') AS DECIMAL(10,2)) AS cafesLisos,
                        TRY_CAST(JSON_VALUE(a.datos_adicionales, '$.helados') AS DECIMAL(10,2)) AS helados,
                        TRY_CAST(JSON_VALUE(a.datos_adicionales, '$.alimonados') AS DECIMAL(10,2)) AS alimonados,
                        TRY_CAST(JSON_VALUE(a.datos_adicionales, '$.revolcados') AS DECIMAL(10,2)) AS revolcados,
                        TRY_CAST(JSON_VALUE(a.datos_adicionales, '$.exportacion') AS DECIMAL(10,2)) AS exportacion,
                        a.datos_adicionales AS datosAdicionales,
                        v.status AS volcadoStatus,
                        v.observaciones AS volcadoObservaciones,
                        v.datos_adicionales AS volcadoDatosAdicionales,
                        br.grano_id AS granoId,
                        g.nombre AS tipoGrano
                    FROM dbo.boletas b
                    LEFT JOIN dbo.analisis_calidad a ON b.analisis_id = a.id
                    LEFT JOIN dbo.bascula_recepciones br ON b.bascula_id = br.id
                    LEFT JOIN dbo.granos_catalogo g ON br.grano_id = g.id
                    LEFT JOIN dbo.boletas_precio bp ON b.id = bp.id
                    LEFT JOIN dbo.volcado_bodega v ON b.bascula_id = v.bascula_id
                    WHERE b.id = @boletaId
                      AND (@sedeId = 0 OR b.sede_id = @sedeId)";

                var detalle = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { boletaId, sedeId });

                if (detalle == null)
                    return NotFound(new { message = "Boleta no encontrada" });

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener detalle de boleta", error = ex.Message });
            }
        }

        // POST: api/volcado/asignar-silo
        [HttpPost("asignar-silo")]
        public async Task<IActionResult> AsignarSilo([FromBody] AsignarSiloDto dto, [FromQuery] int sedeId)
        {
            try
            {
                // 1. Obtener toda la información necesaria de la boleta
                // Incluimos ticket_numero, calibre, humedad y observaciones que faltaban
                var boleta = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        bascula_id, 
                        peso_neto, 
                        ticket_numero, 
                        calibre, 
                        humedad, 
                        observaciones,
                        status 
                    FROM dbo.boletas 
                    WHERE id = @BoletaId",
                    new { dto.BoletaId });

                if (boleta == null)
                    return NotFound(new { message = "Boleta no encontrada" });

                var bodegaId = dto.SiloCalibreId ?? dto.SiloId ?? dto.SiloPulmonId ?? dto.BodegaId;
                if (bodegaId == null)
                    return BadRequest(new { message = "Debe seleccionar al menos un silo o almacén" });

                // Obtener el nombre del silo/almacén seleccionado para guardarlo en bodega_ubicacion
                string siloNombre = "Sin nombre";
                string siloNumero = "";

                if (dto.BodegaId.HasValue)
                {
                    var almacen = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT nombre_almacen AS nombre, CAST(id AS VARCHAR) AS numero FROM dbo.catalogo_almacenes WHERE id = @Id",
                        new { Id = dto.BodegaId.Value });
                    if (almacen != null)
                    {
                        siloNombre = (string)almacen.nombre;
                        siloNumero = (string)almacen.numero;
                    }
                }
                else if (dto.SiloCalibreId.HasValue)
                {
                    var siloCalibre = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT nombre, CAST(id AS VARCHAR) AS numero FROM dbo.silos_calibre_catalogo WHERE id = @Id",
                        new { Id = dto.SiloCalibreId.Value });
                    if (siloCalibre != null)
                    {
                        siloNombre = (string)siloCalibre.nombre;
                        siloNumero = (string)siloCalibre.numero;
                    }
                }
                else if (dto.SiloPulmonId.HasValue)
                {
                    var siloPulmon = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT nombre, CAST(id AS VARCHAR) AS numero FROM dbo.silos_pulmon_catalogo WHERE id = @Id",
                        new { Id = dto.SiloPulmonId.Value });
                    if (siloPulmon != null)
                    {
                        siloNombre = (string)siloPulmon.nombre;
                        siloNumero = (string)siloPulmon.numero;
                    }
                }

                var usuario = Request.Headers["X-User-Email"].ToString();
                var usuarioId = await _db.QueryFirstOrDefaultAsync<int?>(@"SELECT id FROM dbo.usuarios WHERE email = @email", new { email = usuario }) ?? 1;

                var volcadoExistente = await _db.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT id FROM dbo.volcado_bodega WHERE bascula_id = @BasculaId",
                    new { BasculaId = (int)boleta.bascula_id });

                if (volcadoExistente.HasValue)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.volcado_bodega
                        SET bodega_id = @BodegaId,
                            bodega_ubicacion = @BodegaUbicacion,
                            silo_numero = @SiloNumero,
                            kg_volcados = @KgVolcados,
                            ticket_numero = @Ticket,
                            calibre = @Calibre,
                            humedad_verificacion = @Humedad,
                            observaciones = @Obs,
                            status = 'Con Silo Asignado',
                            fecha_hora_volcado = SYSDATETIMEOFFSET(),
                            operador_usuario_id = @UsuarioId,
                            updated_at = SYSDATETIMEOFFSET()
                        WHERE bascula_id = @BasculaId",
                        new {
                            BodegaId = bodegaId,
                            BodegaUbicacion = siloNombre,
                            SiloNumero = siloNumero,
                            KgVolcados = 0,
                            Ticket = boleta.ticket_numero,
                            Calibre = boleta.calibre,
                            Humedad = boleta.humedad,
                            Obs = boleta.observaciones,
                            UsuarioId = usuarioId,
                            BasculaId = (int)boleta.bascula_id
                        });
                }
                else
                {
                    await _db.ExecuteAsync(@"
                        INSERT INTO dbo.volcado_bodega
                        (bascula_id, bodega_id, bodega_ubicacion, silo_numero, fecha_hora_volcado, kg_volcados, operador_usuario_id,
                        created_at, updated_at, sede_id, ticket_numero, calibre, humedad_verificacion, observaciones, status)
                        VALUES
                        (@BasculaId, @BodegaId, @BodegaUbicacion, @SiloNumero, SYSDATETIMEOFFSET(), @KgVolcados, @UsuarioId,
                        SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), @SedeId, @Ticket, @Calibre, @Humedad, @Obs, 'Con Silo Asignado')",
                        new {
                            BasculaId = (int)boleta.bascula_id,
                            BodegaId = bodegaId,
                            BodegaUbicacion = siloNombre,
                            SiloNumero = siloNumero,
                            KgVolcados = 0,
                            UsuarioId = usuarioId,
                            SedeId = sedeId,
                            Ticket = boleta.ticket_numero,
                            Calibre = boleta.calibre,
                            Humedad = boleta.humedad,
                            Obs = boleta.observaciones
                        });
                }

                // Actualizar estados en tablas maestras
                await _db.ExecuteAsync(@"UPDATE dbo.boletas SET status = 'Precio Aceptado', updated_at = SYSDATETIMEOFFSET() WHERE id = @BoletaId", new { dto.BoletaId });
                await _db.ExecuteAsync(@"UPDATE dbo.bascula_recepciones SET status = 'VOLCADO', updated_at = GETDATE() WHERE id = @BasculaId", new { BasculaId = (int)boleta.bascula_id });

                return Ok(new { message = "Silo asignado y datos de boleta sincronizados" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al asignar silo", error = ex.Message });
            }
        }

        [HttpPost("rechazar")]
        public async Task<IActionResult> Rechazar([FromBody] RechazoVolcadoDto dto, [FromQuery] int sedeId)
        {
            try
            {
                // 1. Obtener información de la boleta (Igual que en AsignarSilo)
                var boleta = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT bascula_id, ticket_numero, calibre, humedad, observaciones
                    FROM dbo.boletas 
                    WHERE id = @BoletaId", 
                    new { dto.BoletaId });

                if (boleta == null)
                    return NotFound(new { message = "Boleta no encontrada" });

                // 2. Obtener UsuarioId desde el header (Igual que en AsignarSilo)
                var usuarioEmail = Request.Headers["X-User-Email"].ToString();
                var usuarioId = await _db.QueryFirstOrDefaultAsync<int?>(@"SELECT id FROM dbo.usuarios WHERE email = @email", new { email = usuarioEmail }) ?? 1;

                // 3. Verificar si ya existe en volcado_bodega
                var volcadoExistente = await _db.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT id FROM dbo.volcado_bodega WHERE bascula_id = @BasculaId",
                    new { BasculaId = (int)boleta.bascula_id });

                // Concatenamos el motivo y una marca de la foto en observaciones
                string obsFinal = $"MOTIVO RECHAZO: {dto.Motivos} | (Evidencia adjunta)";

                if (volcadoExistente.HasValue)
                {
                    await _db.ExecuteAsync(@"
                        UPDATE dbo.volcado_bodega
                        SET status = 'Rechazado',
                            observaciones = @obsFinal,
                            datos_adicionales = @DatosAdicionales,
                            operador_usuario_id = @UsuarioId,
                            updated_at = SYSDATETIMEOFFSET()
                        WHERE bascula_id = @BasculaId",
                        new {
                            obsFinal,
                            DatosAdicionales = dto.Evidencia,
                            UsuarioId = usuarioId,
                            BasculaId = (int)boleta.bascula_id
                        });
                }
                else
                {
                    await _db.ExecuteAsync(@"
                        INSERT INTO dbo.volcado_bodega
                        (bascula_id, fecha_hora_volcado, operador_usuario_id, created_at, updated_at, sede_id,
                        ticket_numero, calibre, humedad_verificacion, observaciones, status, kg_volcados, datos_adicionales)
                        VALUES
                        (@BasculaId, SYSDATETIMEOFFSET(), @UsuarioId, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), @SedeId,
                        @Ticket, @Calibre, @Humedad, @obsFinal, 'Rechazado', 0, @DatosAdicionales)",
                        new {
                            BasculaId = (int)boleta.bascula_id,
                            UsuarioId = usuarioId,
                            SedeId = sedeId,
                            Ticket = boleta.ticket_numero,
                            Calibre = boleta.calibre,
                            Humedad = boleta.humedad,
                            obsFinal,
                            DatosAdicionales = dto.Evidencia
                        });
                }

                // 4. Actualizar Tablas Maestras (Lógica de Negocio)
                // Estatus 'En Renegociacion' para que Gerencia lo vea
                await _db.ExecuteAsync(@"
                    UPDATE dbo.boletas 
                    SET status = 'Precio Aceptado', 
                        observaciones = ISNULL(observaciones, '') + ' | ' + @obsFinal,
                        updated_at = SYSDATETIMEOFFSET() 
                    WHERE id = @BoletaId", 
                    new { dto.BoletaId, obsFinal });

                await _db.ExecuteAsync(@"
                    UPDATE dbo.bascula_recepciones 
                    SET status = 'RECHAZADO', 
                        updated_at = GETDATE() 
                    WHERE id = @BasculaId", 
                    new { BasculaId = (int)boleta.bascula_id });

                return Ok(new { message = "Rechazo registrado con éxito" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al rechazar", error = ex.Message });
            }
        }

        

    }

    public class AsignarSiloDto
    {
        public int BoletaId { get; set; }
        public int? SiloId { get; set; }
        public int? SiloCalibreId { get; set; }
        public int? SiloPulmonId { get; set; }
        public int? BodegaId { get; set; }
    }

    // DTO para recibir la petición
    public class RechazoVolcadoDto
    {
        public int BoletaId { get; set; }
        public string Motivos { get; set; }
        public string Evidencia { get; set; }
    }

    // Clase para recibir el JSON del frontend
    public class RechazoRequest
    {
        public int BoletaId { get; set; }
        public string Motivos { get; set; }
        public string Evidencia { get; set; } // Recibe el Base64 de la foto 
    }
}