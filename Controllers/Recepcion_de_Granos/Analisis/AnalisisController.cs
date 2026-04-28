using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient; // Asegúrate de tener este usando o Microsoft.Data.SqlClient
using Dapper;
using SistemaAlazan.Models;
using System.Transactions;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("[controller]")]
    public class AnalisisController : ControllerBase
    {
        private readonly IDbConnection _db;

        public AnalisisController(IDbConnection db)
        {
            _db = db;
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarAnalisis([FromBody] AnalisisCalidadDto dto, [FromQuery] int sedeId)
        {
            // IMPORTANTE: Usar sedeId del query parameter (sede seleccionada)
            dto.SedeId = sedeId;

            // Capturar el usuario que registra el analisis
            // X-User-Email trae el nombre del usuario (ej: "Elias Villegas")
            var nombreUsuario = Request.Headers["X-User-Email"].ToString();
            if (string.IsNullOrEmpty(nombreUsuario))
                nombreUsuario = "sistema";

            // Verificamos que la conexión esté abierta para la transacción
            if (_db.State == ConnectionState.Closed) _db.Open();

            using var transaction = _db.BeginTransaction();

            try
            {
                // Validar que la báscula existe y pertenece a la sede del usuario
                var bascula = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, sede_id FROM bascula_recepciones WHERE id = @id",
                    new { id = dto.BasculaId },
                    transaction
                );

                if (bascula == null)
                {
                    transaction.Rollback();
                    return BadRequest(new {
                        message = "La báscula no existe en la base de datos",
                        bascula_id = dto.BasculaId
                    });
                }

                // Validar que la báscula pertenece a la sede del análisis (protección cruzada)
                if (sedeId != 0 && bascula.sede_id != sedeId)
                {
                    transaction.Rollback();
                    return Forbid(); // 403 Forbidden
                }


                // 1. Insertar el análisis de calidad
                // NOTA: suma_r2 y total_danos son columnas calculadas (computed columns)
                // por lo que SQL Server las calcula automáticamente
                string sqlAnalisis = @"
                    INSERT INTO analisis_calidad (
                        bascula_id, calibre, humedad, impurezas,
                        r1_danado_insecto, r2_quebrado, r2_manchado, r2_arrugado,
                        analista_usuario_id, observaciones, sede_id,
                        datos_adicionales, grano_id, created_at, updated_at
                    ) VALUES (
                        @BasculaId, @Calibre, @Humedad, @Impurezas,
                        @R1DanadoInsecto, @R2Quebrado, @R2Manchado, @R2Arrugado,
                        @AnalistaUsuarioId, @Observaciones, @SedeId,
                        @DatosAdicionales, @GranoId, GETDATE(), GETDATE()
                    );";

                // En Dapper pasamos la transacción como parámetro
                await _db.ExecuteAsync(sqlAnalisis, dto, transaction);

                // 2. Actualizar el estatus de la báscula a 'ANALIZADO'
                string sqlUpdateBascula = @"
                    UPDATE bascula_recepciones
                    SET status = 'ANALIZADO',
                        updated_at = GETDATE()
                    WHERE id = @BasculaId";

                await _db.ExecuteAsync(sqlUpdateBascula, new { dto.BasculaId }, transaction);

                // 3. Crear registro de precio en Boletas_Precio
                // Obtener datos de la báscula y productor
                var datosBascula = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT
                        b.ticket_numero,
                        b.peso_bruto_kg,
                        b.tara_kg,
                        b.ton_aprox,
                        b.placas,
                        b.chofer,
                        CASE
                                WHEN p.tipo_persona = 'Moral' THEN p.atiende
                                WHEN p.tipo_persona = 'Fisica' THEN p.nombre
                                ELSE p.nombre
                            END AS productor_nombre,
                        p.telefono as productor_telefono,
                        ISNULL(o.municipio, '') as productor_origen,
                        b.grano_id,
                        b.sede_id,
                        JSON_VALUE(b.datos_adicionales, '$.tipo_productor') AS tipo_productor
                    FROM bascula_recepciones b
                    INNER JOIN productores p ON b.productor_id = p.id
                    LEFT JOIN origenes_catalogo o ON b.origen_id = o.id
                    WHERE b.id = @BasculaId",
                    new { dto.BasculaId },
                    transaction
                );

                if (datosBascula != null)
                {
                    // Calcular peso neto y toneladas (manejar valores null)
                    decimal pesoBruto = datosBascula.peso_bruto_kg ?? 0;
                    decimal tara = datosBascula.tara_kg ?? 0;
                    decimal pesoNeto = pesoBruto - tara;
                    // Usar ton_aprox declarado por el productor; si no existe, calcular de báscula
                    decimal tonAproxDeclarado = (decimal?)datosBascula.ton_aprox ?? 0m;
                    decimal toneladas = tonAproxDeclarado > 0 ? tonAproxDeclarado : pesoNeto / 1000m;

                    // Obtener comprador desde configuración del sistema
                    var comprador = await _db.QueryFirstOrDefaultAsync<string>(
                        @"SELECT TOP 1 nombre 
                        FROM dbo.compradores_catalogo 
                        WHERE sede_id = @SedeId 
                            AND id = (
                                SELECT comprador_id 
                                FROM bascula_recepciones 
                                WHERE id = @BasculaId
                            )",
                        new
                        {
                            SedeId = datosBascula.sede_id,
                            BasculaId = dto.BasculaId
                        },
                        transaction
                    ) ?? "ALAZAN";


                    // Determinar si el grano es Frijol
                    var granoNombreCheck = await _db.QueryFirstOrDefaultAsync<string>(@"
                        SELECT g.nombre FROM granos_catalogo g
                        WHERE g.id = @GranoId",
                        new { GranoId = (int)(datosBascula.grano_id ?? 0) },
                        transaction
                    ) ?? "";
                    bool esFrijol = granoNombreCheck.Equals("Frijol", StringComparison.OrdinalIgnoreCase);

                    decimal precioSugerido = 0;
                    string precioSugeridoCodigo = "SIN-CONFIG";
                    // true = precio ya está en $/kg (no necesita dividir entre 1000)
                    bool precioYaEnKg = false;

                    bool esGarbanzo = granoNombreCheck.Contains("Garbanzo", StringComparison.OrdinalIgnoreCase);

                    if (esFrijol)
                    {
                        // FRIJOL: descuento_kg_ton está en $/kg, NO dividir entre 1000
                        if (!string.IsNullOrEmpty(dto.Calibre))
                        {
                            var precioCalibreFrijol = await _db.QueryFirstOrDefaultAsync<decimal?>(@"
                                SELECT descuento_kg_ton FROM dbo.DescuentosCalibre_Catalogo
                                WHERE sede_id = @SedeId AND calibre = @Calibre AND grano_id = @GranoId",
                                new { SedeId = datosBascula.sede_id, Calibre = dto.Calibre, GranoId = (int)(datosBascula.grano_id ?? 0) },
                                transaction
                            );
                            precioSugerido = precioCalibreFrijol ?? 0;
                            precioSugeridoCodigo = dto.Calibre;
                            precioYaEnKg = true;
                        }
                    }
                    else if (esGarbanzo)
                    {
                        // GARBANZO: Precios por Clasificación (CAL.EXP, CAL 1, CAL 2)
                        // Precio = Σ (pct_clasificacion/100 × precio_kg_clasificacion)
                        var clasificaciones = await _db.QueryAsync<dynamic>(@"
                            SELECT codigo, precio_kg
                            FROM dbo.precios_clasificacion
                            WHERE sede_id = @SedeId AND grano_id = @GranoId AND activo = 1",
                            new { SedeId = datosBascula.sede_id, GranoId = (int)(datosBascula.grano_id ?? 0) },
                            transaction
                        );

                        if (clasificaciones.Any() && !string.IsNullOrEmpty(dto.DatosAdicionales))
                        {
                            try
                            {
                                var datosJson = System.Text.Json.JsonDocument.Parse(dto.DatosAdicionales);
                                var precioTotal = 0m;
                                var pctTotal = 0m;

                                foreach (var cls in clasificaciones)
                                {
                                    string clave = ((string)(cls.codigo ?? "")).ToLower().Replace(".", "").Replace(" ", "_");
                                    // CAL_EXP se guarda como 'exportacion' en datos_adicionales (es el % de Exportación)
                                    bool esCalExp = clave.Contains("exp");
                                    System.Text.Json.JsonElement pctEl;
                                    bool found = datosJson.RootElement.TryGetProperty(clave, out pctEl);
                                    if (!found && esCalExp)
                                        found = datosJson.RootElement.TryGetProperty("exportacion", out pctEl);
                                    if (found)
                                    {
                                        decimal pct = 0m;
                                        if (pctEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                                            pct = pctEl.GetDecimal();
                                        else if (pctEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                            decimal.TryParse(pctEl.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out pct);
                                        precioTotal += (pct / 100m) * (decimal)(cls.precio_kg ?? 0);
                                        pctTotal += pct;
                                    }
                                }

                                // Si no se enviaron porcentajes, usar el precio de CAL.EXP como referencia
                                if (pctTotal == 0m)
                                {
                                    var calExp = clasificaciones.FirstOrDefault(c =>
                                        ((string)(c.codigo ?? "")).Contains("EXP", StringComparison.OrdinalIgnoreCase));
                                    if (calExp != null) precioTotal = (decimal)(calExp.precio_kg ?? 0);
                                }

                                precioSugerido = precioTotal;
                                precioSugeridoCodigo = "CLASIF";
                                precioYaEnKg = true;
                            }
                            catch
                            {
                                precioSugerido = 0;
                            }
                        }
                    }
                    else
                    {
                        // Otros granos: mantener lógica P1-P27 (MXN/ton → dividir entre 1000 al insertar)
                        decimal porcentajeExportacion = 0;
                        if (!string.IsNullOrEmpty(dto.DatosAdicionales))
                        {
                            try
                            {
                                var datosJson = System.Text.Json.JsonDocument.Parse(dto.DatosAdicionales);
                                if (datosJson.RootElement.TryGetProperty("exportacion", out var exportacionElement))
                                    porcentajeExportacion = exportacionElement.GetDecimal();
                            }
                            catch { }
                        }

                        var nivelesPrecio = await _db.QueryAsync<dynamic>(@"
                            SELECT n.id, n.codigo, n.precio_final_mxn, n.descuento_precio_id
                            FROM NivelesPrecioExportacion n
                            WHERE n.sede_id = @SedeId AND n.vigente = 1
                            ORDER BY TRY_CAST(REPLACE(n.codigo, 'P', '') AS INT) ASC",
                            new { SedeId = datosBascula.sede_id }, transaction);

                        var descuentosPrecio = await _db.QueryAsync<dynamic>(@"
                            SELECT id, descuento_mxn FROM dbo.DescuentosPrecio_Catalogo WHERE sede_id = @SedeId",
                            new { SedeId = datosBascula.sede_id }, transaction);

                        var descuentosMap = new Dictionary<int, decimal>();
                        foreach (var d in descuentosPrecio)
                            descuentosMap[(int)d.id] = (decimal)(d.descuento_mxn ?? 0);

                        decimal descuentoCalibre = 0;
                        if (!string.IsNullOrEmpty(dto.Calibre))
                        {
                            var calibreDesc = await _db.QueryFirstOrDefaultAsync<decimal?>(@"
                                SELECT descuento_kg_ton FROM dbo.DescuentosCalibre_Catalogo
                                WHERE sede_id = @SedeId AND calibre = @Calibre",
                                new { SedeId = datosBascula.sede_id, Calibre = dto.Calibre }, transaction);
                            descuentoCalibre = calibreDesc ?? 0;
                        }

                        var preciosCalculados = new Dictionary<string, decimal>();
                        decimal precioAcumulado = 0;
                        int indice = 0;
                        foreach (var nivel in nivelesPrecio)
                        {
                            string cod = nivel.codigo ?? "";
                            if (indice == 0)
                                precioAcumulado = (decimal)(nivel.precio_final_mxn ?? 0) - descuentoCalibre;
                            int? descuentoId = (int?)nivel.descuento_precio_id;
                            if (descuentoId.HasValue && descuentosMap.ContainsKey(descuentoId.Value))
                                precioAcumulado -= descuentosMap[descuentoId.Value];
                            if (precioAcumulado < 0) precioAcumulado = 0;
                            preciosCalculados[cod] = precioAcumulado;
                            indice++;
                        }

                        foreach (var nivel in nivelesPrecio)
                        {
                            string codigo = nivel.codigo ?? "";
                            int porcentajeMinimo = 0;
                            var match = System.Text.RegularExpressions.Regex.Match(codigo, @"P(\d+)");
                            if (match.Success)
                                porcentajeMinimo = 81 - int.Parse(match.Groups[1].Value);
                            if (porcentajeExportacion >= porcentajeMinimo)
                            {
                                precioSugerido = preciosCalculados.ContainsKey(codigo) ? preciosCalculados[codigo] : 0;
                                precioSugeridoCodigo = codigo;
                                break;
                            }
                        }

                        if (precioSugerido == 0 && preciosCalculados.Any())
                        {
                            var ultimo = preciosCalculados.Last();
                            precioSugerido = ultimo.Value;
                            precioSugeridoCodigo = ultimo.Key;
                        }
                    }

                    if (precioSugerido < 0) precioSugerido = 0;

                    // Obtener factor_impurezas para calcular descuento de boletas_precio
                    var factorImpurezas = await _db.QueryFirstOrDefaultAsync<decimal?>(@"
                        SELECT factor_impurezas FROM dbo.Configuracion_Recepcion_Reglas
                        WHERE sede_id = @SedeId",
                        new { SedeId = datosBascula.sede_id },
                        transaction
                    ) ?? 0;
                    decimal descuentoPrecio = factorImpurezas * dto.Impurezas;

                    // Insertar en boletas
                    // NOTA: kg_a_liquidar e importe_total se calculan en preliquidación, no aquí

                    // Obtener precio base en USD y tipo de cambio desde precios_configuracion
                    var precioConfig = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT TOP 1 id, precio_base_usd_ton, tipo_cambio_fix
                        FROM precios_configuracion
                        WHERE activo = 1
                        ORDER BY fecha_vigencia DESC",
                        transaction: transaction
                    );

                    decimal precioBaseUsd = precioConfig?.precio_base_usd_ton ?? 0;
                    decimal tipoCambio = precioConfig?.tipo_cambio_fix ?? 0;

                    // Obtener el ID del análisis recién insertado
                    var analisisId = await _db.QueryFirstOrDefaultAsync<int?>(@"
                        SELECT TOP 1 id
                        FROM analisis_calidad
                        WHERE bascula_id = @BasculaId
                        ORDER BY created_at DESC",
                        new { dto.BasculaId },
                        transaction
                    );

                    // Obtener el productor_id desde bascula_recepciones
                    var productorId = await _db.QueryFirstOrDefaultAsync<int?>(@"
                        SELECT productor_id FROM bascula_recepciones WHERE id = @BasculaId",
                        new { dto.BasculaId },
                        transaction
                    );

                    // Usar el nombre del grano ya obtenido
                    var granoNombre = string.IsNullOrEmpty(granoNombreCheck) ? "Garbanzo" : granoNombreCheck;

                    string sqlBoleta = @"
                        INSERT INTO boletas (
                            sede_id, folio, ticket_numero, bascula_id, analisis_id,
                            fecha_hora, productor, telefono, comprador, origen, calibre,
                            humedad, peso_bruto, tara, peso_neto,
                            precio_base_usd, tipo_cambio, precio_mxn,
                            descuento_kg_ton, kg_a_liquidar, importe_total,
                            t_productor,
                            observaciones, status, created_at, updated_at
                        )
                        OUTPUT INSERTED.id
                        VALUES (
                            @SedeId, @Folio, @TicketNumero, @BasculaId, @AnalisisId,
                            SYSDATETIMEOFFSET(), @Productor, @Telefono, @Comprador, @Origen, @Calibre,
                            @Humedad, @PesoBruto, @Tara, @PesoNeto,
                            @PrecioBaseUsd, @TipoCambio, @PrecioMxn,
                            @DescuentoKgTon, @KgALiquidar, @ImporteTotal,
                            @TProductor,
                            @Observaciones, @Status, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                        )";

                    var boletaId = await _db.ExecuteScalarAsync<int>(sqlBoleta, new
                    {
                        SedeId = datosBascula.sede_id,
                        Folio = datosBascula.ticket_numero,
                        TicketNumero = datosBascula.ticket_numero,
                        BasculaId = dto.BasculaId,
                        AnalisisId = analisisId,
                        Productor = datosBascula.productor_nombre,
                        Telefono = datosBascula.productor_telefono ?? "",
                        Comprador = comprador,
                        Origen = datosBascula.productor_origen ?? "",
                        Calibre = dto.Calibre,
                        Humedad = dto.Humedad,
                        PesoBruto = pesoBruto,
                        Tara = tara,
                        PesoNeto = 0m,   // El peso neto real se calcula en preliquidación (tara aún desconocida)
                        PrecioBaseUsd = precioBaseUsd,
                        TipoCambio = tipoCambio,
                        PrecioMxn = precioYaEnKg ? precioSugerido : precioSugerido / 1000m,
                        DescuentoKgTon = descuentoPrecio,
                        KgALiquidar = 0m,
                        ImporteTotal = 0m,
                        TProductor = (string)(datosBascula.tipo_productor ?? ""),
                        Observaciones = dto.Observaciones ?? "",
                        Status = "Sin Precio"
                    }, transaction);

                    // 4. Insertar en boletas_precio (tabla dedicada para el modulo de Precio)
                    string sqlBoletaPrecio = @"
                        INSERT INTO boletas_precio (
                            sede_id, no_boleta, ticket, fecha_registro,
                            productor_id, productor_nombre, telefono, comprador, origen,
                            calibre, tipo_grano, peso_bruto, tons_aprox,
                            descuento_kg_ton, precio_sugerido, precio_sugerido_codigo,
                            estatus, tiempo_registro, es_de_analisis,
                            usuario_registro, boleta_id, observaciones
                        )
                        VALUES (
                            @SedeId, @NoBoleta, @Ticket, SYSDATETIMEOFFSET(),
                            @ProductorId, @ProductorNombre, @Telefono, @Comprador, @Origen,
                            @Calibre, @TipoGrano, @PesoBruto, @TonsAprox,
                            @DescuentoKgTon, @PrecioSugerido, @PrecioSugeridoCodigo,
                            'Sin Precio', SYSDATETIMEOFFSET(), 1,
                            @UsuarioRegistro, @BoletaId, @Observaciones
                        )";

                    await _db.ExecuteAsync(sqlBoletaPrecio, new
                    {
                        SedeId = datosBascula.sede_id,
                        NoBoleta = datosBascula.ticket_numero,
                        Ticket = datosBascula.ticket_numero,
                        ProductorId = productorId,
                        ProductorNombre = datosBascula.productor_nombre ?? "",
                        Telefono = datosBascula.productor_telefono ?? "",
                        Comprador = comprador,
                        Origen = datosBascula.productor_origen ?? "",
                        Calibre = dto.Calibre,
                        TipoGrano = granoNombre,
                        PesoBruto = pesoBruto,
                        TonsAprox = toneladas,
                        DescuentoKgTon = descuentoPrecio,
                        PrecioSugerido = precioYaEnKg ? precioSugerido : precioSugerido / 1000m,
                        PrecioSugeridoCodigo = precioSugeridoCodigo,
                        UsuarioRegistro = nombreUsuario,
                        BoletaId = boletaId,
                        Observaciones = dto.Observaciones ?? ""
                    }, transaction);
                }

                transaction.Commit();
                return Ok(new { message = "Análisis registrado correctamente y boleta de precio generada" });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { message = "Error al procesar el análisis", details = ex.Message });
            }
        }

        [HttpGet("pendientes-analisis")]
        public async Task<IActionResult> GetPendientes(
            [FromQuery] int sedeId,
            [FromQuery] string? estatus = null,
            [FromQuery] string? fechaInicio = null,
            [FromQuery] string? fechaFin = null)
        {
            try
            {
                // Si no se especifica estatus, solo traer PENDIENTE (comportamiento original)
                var filtroEstatus = string.IsNullOrEmpty(estatus) ? "PENDIENTE" : estatus;

                var sql = @"
                SELECT
                b.id, b.ticket_numero, b.fecha_hora as fecha, b.chofer, b.placas,
                CASE
                WHEN p.tipo_persona = 'Moral' THEN p.atiende
                WHEN p.tipo_persona = 'Fisica' THEN p.nombre
                ELSE NULL
                END AS productor,
                b.peso_bruto_kg, 
                b.status,
                a.calibre, 
                a.humedad, 
                a.impurezas, 
                a.r1_danado_insecto,
                a.r2_arrugado, 
                a.r2_manchado, 
                a.r2_quebrado,
                a.datos_adicionales,
                b.grano_id,
                g.nombre AS grano
                FROM bascula_recepciones b
                JOIN productores p ON b.productor_id = p.id
                LEFT JOIN analisis_calidad a ON b.id = a.bascula_id
                LEFT JOIN granos_catalogo g ON b.grano_id = g.id
                WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                AND (@filtroEstatus = 'TODOS'
                OR (@filtroEstatus = 'NO_PENDIENTE' AND b.status <> 'PENDIENTE')
                OR b.status = @filtroEstatus)
                AND (@fechaInicio IS NULL OR CAST(b.fecha_hora AS DATE) >= CAST(@fechaInicio AS DATE))
                AND (@fechaFin IS NULL OR CAST(b.fecha_hora AS DATE) <= CAST(@fechaFin AS DATE))
                ORDER BY b.fecha_hora DESC";

                var pendientes = await _db.QueryAsync<object>(sql, new
                {
                    sedeId,
                    filtroEstatus,
                    fechaInicio = string.IsNullOrEmpty(fechaInicio) ? null : fechaInicio,
                    fechaFin = string.IsNullOrEmpty(fechaFin) ? null : fechaFin
                });
                return Ok(pendientes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener registros: {ex.Message}");
            }
        }

        [HttpPut("actualizar/{basculaId}")]
        public async Task<IActionResult> ActualizarAnalisis(int basculaId, [FromBody] AnalisisCalidadDto dto)
        {
            try
            {
                // Validar que el análisis pertenece a la sede del usuario
                var sedeAnalisis = await _db.QueryFirstOrDefaultAsync<int?>(
                    "SELECT sede_id FROM analisis_calidad WHERE bascula_id = @basculaId",
                    new { basculaId }
                );

                if (sedeAnalisis == null)
                {
                    return NotFound(new { message = "No se encontró el análisis para actualizar" });
                }

                // Validar acceso cruzado de sedes (solo admin global puede actualizar cualquier sede)
                if (dto.SedeId != 0 && sedeAnalisis != dto.SedeId)
                {
                    return Forbid(); // 403 Forbidden
                }

                // 1. Verificamos que los nombres de las propiedades en el objeto anónimo
                // coincidan con los nombres definidos en tu AnalisisCalidadDto.
                var sql = @"
                    UPDATE analisis_calidad
                    SET calibre = @Calibre,
                        humedad = @Humedad,
                        impurezas = @Impurezas,
                        r1_danado_insecto = @R1DanadoInsecto,
                        r2_quebrado = @R2Quebrado,
                        r2_manchado = @R2Manchado,
                        r2_arrugado = @R2Arrugado,
                        datos_adicionales = @DatosAdicionales,
                        grano_id = @GranoId,
                        updated_at = GETDATE()
                    WHERE bascula_id = @basculaId";

                // Asegúrate de que las propiedades del DTO (Calibre, Humedad, etc.)
                // empiecen con Mayúscula si así están en tu clase AnalisisCalidadDto
                int filasAfectadas = await _db.ExecuteAsync(sql, new {
                    Calibre = dto.Calibre,
                    Humedad = dto.Humedad,
                    Impurezas = dto.Impurezas,
                    R1DanadoInsecto = dto.R1DanadoInsecto,
                    R2Quebrado = dto.R2Quebrado,
                    R2Manchado = dto.R2Manchado,
                    R2Arrugado = dto.R2Arrugado,
                    DatosAdicionales = dto.DatosAdicionales,
                    GranoId = dto.GranoId,
                    basculaId = basculaId
                });

                if (filasAfectadas == 0)
                {
                    return NotFound(new { message = "No se encontró el análisis para actualizar" });
                }

                // Asegurar que el estatus quede en ANALIZADO aunque venga de PENDIENTE
                await _db.ExecuteAsync(@"
                    UPDATE bascula_recepciones
                    SET status = 'ANALIZADO', updated_at = GETDATE()
                    WHERE id = @basculaId AND status = 'PENDIENTE'",
                    new { basculaId });

                return Ok(new { message = "Análisis actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al actualizar", details = ex.Message });
            }
        }

        [HttpGet("detalles/{basculaId}")]
        public async Task<IActionResult> GetDetallesAnalisis(int basculaId, [FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT
                        b.id,
                        b.ticket_numero,
                        b.chofer,
                        b.placas,
                        b.fecha_hora as fecha,
                        CASE
                                WHEN p.tipo_persona = 'Moral' THEN p.atiende
                                WHEN p.tipo_persona = 'Fisica' THEN p.nombre
                                ELSE NULL
                            END AS productor,
                        a.calibre,
                        a.humedad,
                        a.impurezas,
                        a.r1_danado_insecto,
                        a.r2_arrugado,
                        a.r2_manchado,
                        a.r2_quebrado,
                        a.datos_adicionales,
                        a.sede_id
                    FROM bascula_recepciones b
                    JOIN productores p ON b.productor_id = p.id
                    INNER JOIN analisis_calidad a ON b.id = a.bascula_id
                    WHERE b.id = @basculaId";

                var detalle = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { basculaId });

                if (detalle == null)
                    return NotFound(new { message = "No se encontró el análisis solicitado." });

                // Validar acceso cruzado de sedes (solo admin global puede ver cualquier sede)
                if (sedeId != 0 && detalle.sede_id != sedeId)
                {
                    return Forbid(); // 403 Forbidden
                }

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener detalles: {ex.Message}");
            }
        }

    }
}