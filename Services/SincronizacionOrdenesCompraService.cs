using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Alazan.API.Services
{
    public class SincronizacionOrdenesCompraService : BackgroundService
    {
        private const string MBA3_BASE   = "http://201.148.25.52:8443";
        private const string MBA3_CODIGO = "CON100"; // CON100 tiene acceso a tablas CLNT (API100 solo a PROV)
        private const string MBA3_PWD    = "zaqxsw97531";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory   _httpClientFactory;
        private readonly ILogger<SincronizacionOrdenesCompraService> _logger;

        public SincronizacionOrdenesCompraService(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            ILogger<SincronizacionOrdenesCompraService> logger)
        {
            _scopeFactory      = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger            = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de sincronización OC MBA3 iniciado");

            // Retraso inicial: esperar 6 minutos para no competir con peticiones de usuarios al arrancar
            await Task.Delay(TimeSpan.FromMinutes(6), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SincronizarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en sincronización OC MBA3");
                }

                await Task.Delay(TimeSpan.FromMinutes(1440), stoppingToken);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        public async Task SincronizarAsync()
        {
            _logger.LogInformation("Iniciando sincronización de órdenes de compra desde MBA3");

            var jwt = await ObtenerJwtAsync();

            // 1. Fetch órdenes de compra — una sola llamada (JWT single-use)
            var ocs = await ConsultarMba3Async(jwt,
                select: "*",
                from:   "CLNT_Pedidos_Principal",
                where:  "CORP='BGAR1' AND Tipo_Doc_OC_PE_CT='OC' AND ORIGIN='GML'");

            if (ocs == null || ocs.Count == 0)
            {
                _logger.LogInformation("MBA3 no devolvió órdenes de compra activas");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = (SqlConnection)scope.ServiceProvider.GetRequiredService<IDbConnection>();
            if (db.State != ConnectionState.Open) db.Open();

            // 2. Cargar productores locales — usar TryAdd para ignorar duplicados normalizados
            var productoresLista = (await db.QueryAsync<ProductorLocal>(@"
                SELECT id, UPPER(LTRIM(RTRIM(nombre))) AS nombre, codigo_proveedor
                FROM dbo.productores WHERE activo = 1")).ToList();

            var porCodigo = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in productoresLista.Where(p => !string.IsNullOrEmpty(p.CodigoProveedor)))
            {
                var key = p.CodigoProveedor!.TrimStart('0');
                if (!string.IsNullOrEmpty(key))
                    porCodigo.TryAdd(key, p.Id);
            }

            // 3. Fetch PROV_Ficha_Principal y auto-insertar productores faltantes
            var codigosExistentes = new HashSet<string>(porCodigo.Keys, StringComparer.OrdinalIgnoreCase);
            try
            {
                var jwtProv = await ObtenerJwtAsync();
                var provItems = await ConsultarMba3Async(jwtProv,
                    select: "VENDOR_ID,VENDOR_NAME,RUC_or_FED_ID,TELEPHONE_MAIN,TELEPHONE_PM," +
                            "FACSIMILE,E_MAIL,ACCT_CODE,ACCOUNT_MNGR,ADDRESS_1,ADDRESS_2," +
                            "CITY,STATE,ZIP,COUNTRY,NAME_RAZON_SOCIAL",
                    from:   "PROV_Ficha_Principal",
                    where:  "CORP='BGAR1'");

                if (provItems != null)
                {
                    int autoInsertados = 0;
                    foreach (var v in provItems)
                    {
                        var vid = Str(v, "VENDOR_ID");
                        if (string.IsNullOrEmpty(vid)) continue;
                        var vidNorm = vid.TrimStart('0');
                        if (string.IsNullOrEmpty(vidNorm) || codigosExistentes.Contains(vidNorm)) continue;

                        var nombre = Str(v, "VENDOR_NAME");
                        if (string.IsNullOrEmpty(nombre)) continue;

                        var rfc = Str(v, "RUC_or_FED_ID")?.Replace(" ", "");

                        var newId = await db.ExecuteScalarAsync<long>(@"
                            INSERT INTO dbo.productores
                                (nombre, rfc, telefono, correo, telefono2, fax,
                                 codigo_proveedor, atiende, direccion1, direccion2,
                                 ciudad, estado, codigo_postal, pais, nombre_alterno,
                                 origen, activo, created_at, updated_at)
                            OUTPUT INSERTED.id
                            SELECT @nombre, @rfc, @telefono, @correo, @telefono2, @fax,
                                   @codigoProveedor, @atiende, @direccion1, @direccion2,
                                   @ciudad, @estado, @codigoPostal, @pais, @nombreAlterno,
                                   'mba', 1, GETDATE(), GETDATE()
                            WHERE NOT EXISTS (
                                SELECT 1 FROM dbo.productores WHERE codigo_proveedor = @codigoProveedor)",
                            new
                            {
                                nombre,
                                rfc          = string.IsNullOrEmpty(rfc) ? null : rfc,
                                telefono     = Str(v, "TELEPHONE_MAIN")?.Replace(" ", ""),
                                correo       = Str(v, "E_MAIL"),
                                telefono2    = Str(v, "TELEPHONE_PM")?.Replace(" ", ""),
                                fax          = Str(v, "FACSIMILE"),
                                codigoProveedor = vidNorm,
                                atiende      = Str(v, "ACCOUNT_MNGR"),
                                direccion1   = Str(v, "ADDRESS_1"),
                                direccion2   = Str(v, "ADDRESS_2"),
                                ciudad       = Str(v, "CITY"),
                                estado       = Str(v, "STATE"),
                                codigoPostal = Str(v, "ZIP"),
                                pais         = Str(v, "COUNTRY"),
                                nombreAlterno = Str(v, "NAME_RAZON_SOCIAL"),
                            });

                        if (newId > 0)
                        {
                            porCodigo[vidNorm] = newId;
                            codigosExistentes.Add(vidNorm);
                            autoInsertados++;
                        }
                    }
                    if (autoInsertados > 0)
                        _logger.LogInformation("Productores auto-insertados desde PROV_Ficha_Principal: {N}", autoInsertados);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar PROV_Ficha_Principal; se continúa sin auto-insertar productores");
            }

            // 4. Pre-cargar todas las OCs existentes en un diccionario — elimina el N+1 de queries
            var existentesDict = (await db.QueryAsync<OcExistente>(
                "SELECT id, contrato_id_corp, status FROM dbo.mba3_ordenes_compra"))
                .ToDictionary(o => o.ContratoIdCorp, StringComparer.OrdinalIgnoreCase);

            int insertados = 0, actualizados = 0, cambiosStatus = 0;
            var historialPendiente = new List<(int OrdenId, string Anterior, string Nuevo)>();

            _logger.LogInformation("MBA3 devolvió {Total} órdenes de compra para procesar", ocs.Count);

            foreach (var oc in ocs)
            {
                var contratoId  = LongVal(oc, "CONTRATO_ID");
                var corp        = Str(oc, "CONTRATO_ID_CORP") ?? "";
                var clientId    = Str(oc, "CLIENT_ID") ?? "";
                var statusNuevo = Str(oc, "STATUS") ?? "";

                if (string.IsNullOrEmpty(corp))
                {
                    _logger.LogWarning("OC con CONTRATO_ID={Id} ignorada: CONTRATO_ID_CORP vacío o no es string. Raw={Raw}",
                        contratoId, oc.GetRawText()[..Math.Min(200, oc.GetRawText().Length)]);
                    continue;
                }

                var clientIdNorm = clientId.TrimStart('0');
                long? productorId = !string.IsNullOrEmpty(clientIdNorm) && porCodigo.TryGetValue(clientIdNorm, out var pid) ? pid : null;

                var datosJson = oc.GetRawText();

                // Lookup en memoria — sin round-trip a DB por cada OC
                existentesDict.TryGetValue(corp, out var existente);

                if (existente != null && existente.Status != statusNuevo)
                {
                    historialPendiente.Add((existente.Id, existente.Status, statusNuevo));
                    cambiosStatus++;
                }

                var parametros = new
                {
                    contrato_id        = LongVal(oc, "CONTRATO_ID"),
                    contrato_id_corp   = corp,
                    pk_uuid            = Str(oc, "pkUUID"),
                    corp_val           = Str(oc, "CORP"),
                    client_id          = clientId,
                    client_id_corp     = Str(oc, "CLIENT_ID_CORP"),
                    salesman           = Str(oc, "SALESMAN"),
                    fecha_pedido       = DateVal(oc, "FECHA_PEDIDO"),
                    fecha_desde        = DateVal(oc, "FECHA_DESDE"),
                    fecha_hasta        = DateVal(oc, "FECHA_HASTA"),
                    fecha_entrega      = DateVal(oc, "FECHA_ENTREGA"),
                    inv_amount         = DecimalVal(oc, "INV_AMOUNT"),
                    currency_type      = Str(oc, "CURRENCY_TYPE"),
                    descuento_porc     = DecimalVal(oc, "DESCUENTO_PORC"),
                    status             = statusNuevo,
                    confirmed          = BoolVal(oc, "CONFIRMED"),
                    tipo_doc           = Str(oc, "Tipo_Doc_OC_PE_CT"),
                    tipo_orden         = Str(oc, "TIPO_ORDEN_COMPRA"),
                    ware_code          = Str(oc, "WARE_CODE"),
                    referencia_general = Str(oc, "REFERENCIA_GENERAL"),
                    pais_proveedor     = Str(oc, "Pais_Proveedor"),
                    ciudad_proveedor   = Str(oc, "Ciudad_Proveedor"),
                    productor_id       = productorId,
                    datos_json         = datosJson
                };

                await db.ExecuteAsync(@"
                    MERGE dbo.mba3_ordenes_compra AS t
                    USING (SELECT @contrato_id_corp AS k) AS s ON t.contrato_id_corp = s.k
                    WHEN MATCHED THEN UPDATE SET
                        status            = @status,
                        inv_amount        = @inv_amount,
                        fecha_hasta       = @fecha_hasta,
                        fecha_entrega     = @fecha_entrega,
                        productor_id      = COALESCE(@productor_id, t.productor_id),
                        datos_json        = @datos_json,
                        ultima_sync       = GETDATE()
                    WHEN NOT MATCHED THEN INSERT (
                        contrato_id, contrato_id_corp, pk_uuid, corp,
                        client_id, client_id_corp, salesman,
                        fecha_pedido, fecha_desde, fecha_hasta, fecha_entrega,
                        inv_amount, currency_type, descuento_porc,
                        status, confirmed, tipo_doc_oc_pe_ct, tipo_orden_compra, ware_code,
                        referencia_general, pais_proveedor, ciudad_proveedor,
                        productor_id, datos_json
                    ) VALUES (
                        @contrato_id, @contrato_id_corp, @pk_uuid, @corp_val,
                        @client_id, @client_id_corp, @salesman,
                        @fecha_pedido, @fecha_desde, @fecha_hasta, @fecha_entrega,
                        @inv_amount, @currency_type, @descuento_porc,
                        @status, @confirmed, @tipo_doc, @tipo_orden, @ware_code,
                        @referencia_general, @pais_proveedor, @ciudad_proveedor,
                        @productor_id, @datos_json
                    );", parametros);

                if (existente == null) insertados++;
                else actualizados++;
            }

            // 5. Insertar historial en batch (una sola llamada en lugar de una por cambio)
            if (historialPendiente.Count > 0)
            {
                await db.ExecuteAsync(
                    @"INSERT INTO dbo.mba3_ordenes_compra_historial (orden_id, status_anterior, status_nuevo)
                      VALUES (@OrdenId, @Anterior, @Nuevo)",
                    historialPendiente.Select(h => new { h.OrdenId, h.Anterior, h.Nuevo }));
            }

            _logger.LogInformation(
                "Sync OC MBA3 completada. Total MBA3: {T}, Insertadas: {I}, Actualizadas: {A}, Cambios de status: {C}",
                ocs.Count, insertados, actualizados, cambiosStatus);
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task<List<JsonElement>?> ConsultarMba3Async(
            string jwt, string select, string from, string where)
        {
            var client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["select"] = select,
                ["from"]   = from,
                ["where"]  = where,
                ["limit"]  = "9999",
            });

            var req = new HttpRequestMessage(HttpMethod.Post, MBA3_BASE + "/ws_Consulta_externa_MBA3/")
            {
                Content = form
            };
            req.Headers.TryAddWithoutValidation("Authorization", jwt);

            var response = await client.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MBA3 respondió {Code} para from={From}", response.StatusCode, from);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<JsonElement>>(json);
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task<string> ObtenerJwtAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var body = new StringContent(
                JsonSerializer.Serialize(new { codigo = MBA3_CODIGO, pwd = MBA3_PWD }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync(MBA3_BASE + "/login_servicio", body);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("jwt", out var jwtEl))
                throw new InvalidOperationException("MBA3 no devolvió JWT en /login_servicio");

            return jwtEl.GetString() ?? throw new InvalidOperationException("JWT vacío");
        }

        // ─── Helpers de lectura de JsonElement ───────────────────────────────
        private static string? Str(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String)
            {
                var s = p.GetString()?.Trim();
                return string.IsNullOrEmpty(s) ? null : s;
            }
            return null;
        }

        private static long? LongVal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p))
            {
                if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var v)) return v;
                if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var vs)) return vs;
            }
            return null;
        }

        private static decimal? DecimalVal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p))
            {
                if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var v)) return v;
                if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var vs)) return vs;
            }
            return null;
        }

        private static DateTime? DateVal(JsonElement el, string key)
        {
            var s = Str(el, key);
            if (s == null || s.StartsWith("0000")) return null;
            return DateTime.TryParse(s, out var d) ? d : (DateTime?)null;
        }

        private static bool? BoolVal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p))
            {
                if (p.ValueKind == JsonValueKind.True)  return true;
                if (p.ValueKind == JsonValueKind.False) return false;
                if (p.ValueKind == JsonValueKind.String)
                    return p.GetString()?.ToLower() is "true" or "1";
            }
            return null;
        }

        private class ProductorLocal
        {
            public long    Id               { get; set; }
            public string  Nombre           { get; set; } = "";
            public string? CodigoProveedor  { get; set; }
        }

        private class OcExistente
        {
            public int    Id              { get; set; }
            public string ContratoIdCorp  { get; set; } = "";
            public string Status          { get; set; } = "";
        }
    }
}
