using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Alazan.API.Services
{
    public class SincronizacionMba3ProductoresService : BackgroundService
    {
        private const string MBA3_BASE = "http://201.148.25.52:8443";
        private const string MBA3_CODIGO = "API100";
        private const string MBA3_PWD = "zaqxsw97531";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SincronizacionMba3ProductoresService> _logger;

        public SincronizacionMba3ProductoresService(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            ILogger<SincronizacionMba3ProductoresService> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de sincronización MBA3 → productores iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SincronizarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en sincronización MBA3 → productores");
                }

                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
        }

        private async Task SincronizarAsync()
        {
            _logger.LogInformation("Iniciando sincronización de proveedores desde MBA3");

            var jwt = await ObtenerJwtAsync();

            var client = _httpClientFactory.CreateClient();
            var fecha3 = DateTime.UtcNow.AddYears(-3).ToString("yyyy-MM-dd");

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["select"] = "VENDOR_NAME,RUC_or_FED_ID,ACCOUNT_MNGR,TELEPHONE_MAIN,TELEPHONE_PM,ACCT_CODE,ADDRESS_1,ADDRESS_2,CITY,STATE,ZIP,COUNTRY,NAME_RAZON_SOCIAL,FACSIMILE,E_MAIL",
                ["from"]   = "PROV_Ficha_Principal",
                ["where"]  = $"CORP='BGAR1' AND RECORD_DATE > '{fecha3}'",
                ["limit"]  = "9999",
            });

            var req = new HttpRequestMessage(HttpMethod.Post, MBA3_BASE + "/ws_Consulta_externa_MBA3/")
            {
                Content = formContent,
            };
            req.Headers.TryAddWithoutValidation("Authorization", jwt);

            var response = await client.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (items == null || items.Count == 0)
            {
                _logger.LogInformation("MBA3 no devolvió registros de proveedores");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = (SqlConnection)scope.ServiceProvider.GetRequiredService<IDbConnection>();
            if (db.State != ConnectionState.Open) db.Open();

            await db.ExecuteAsync(@"
                CREATE TABLE #tmp_mba3 (
                    nombre          NVARCHAR(300) NULL,
                    rfc             NVARCHAR(20)  NULL,
                    telefono        NVARCHAR(50)  NULL,
                    telefono2       NVARCHAR(50)  NULL,
                    correo          NVARCHAR(200) NULL,
                    fax             NVARCHAR(50)  NULL,
                    codigo_proveedor NVARCHAR(50) NULL,
                    atiende         NVARCHAR(200) NULL,
                    direccion1      NVARCHAR(500) NULL,
                    direccion2      NVARCHAR(500) NULL,
                    ciudad          NVARCHAR(100) NULL,
                    estado          NVARCHAR(100) NULL,
                    codigo_postal   NVARCHAR(10)  NULL,
                    pais            NVARCHAR(10)  NULL,
                    nombre_alterno  NVARCHAR(300) NULL
                )");

            var dt = new DataTable();
            dt.Columns.Add("nombre");
            dt.Columns.Add("rfc");
            dt.Columns.Add("telefono");
            dt.Columns.Add("telefono2");
            dt.Columns.Add("correo");
            dt.Columns.Add("fax");
            dt.Columns.Add("codigo_proveedor");
            dt.Columns.Add("atiende");
            dt.Columns.Add("direccion1");
            dt.Columns.Add("direccion2");
            dt.Columns.Add("ciudad");
            dt.Columns.Add("estado");
            dt.Columns.Add("codigo_postal");
            dt.Columns.Add("pais");
            dt.Columns.Add("nombre_alterno");

            foreach (var item in items)
            {
                var rfc = Str(item, "RUC_or_FED_ID")?.Replace(" ", "") ?? "";
                dt.Rows.Add(
                    Str(item, "VENDOR_NAME"),
                    string.IsNullOrEmpty(rfc) ? (object)DBNull.Value : rfc,
                    Str(item, "TELEPHONE_MAIN")?.Replace(" ", ""),
                    Str(item, "TELEPHONE_PM")?.Replace(" ", ""),
                    Str(item, "E_MAIL"),
                    Str(item, "FACSIMILE"),
                    Str(item, "ACCT_CODE"),
                    Str(item, "ACCOUNT_MNGR"),
                    Str(item, "ADDRESS_1"),
                    Str(item, "ADDRESS_2"),
                    Str(item, "CITY"),
                    Str(item, "STATE"),
                    Str(item, "ZIP"),
                    Str(item, "COUNTRY"),
                    Str(item, "NAME_RAZON_SOCIAL")
                );
            }

            using (var bulk = new SqlBulkCopy(db))
            {
                bulk.DestinationTableName = "#tmp_mba3";
                await bulk.WriteToServerAsync(dt);
            }

            var result = await db.QueryFirstAsync<dynamic>(@"
                DECLARE @output TABLE (accion NVARCHAR(10));

                MERGE dbo.productores AS t
                USING (
                    SELECT * FROM (
                        SELECT *, ROW_NUMBER() OVER (PARTITION BY rfc ORDER BY (SELECT NULL)) AS rn
                        FROM #tmp_mba3
                        WHERE rfc IS NOT NULL AND rfc != ''
                    ) x WHERE rn = 1
                ) AS s ON t.rfc = s.rfc
                WHEN MATCHED THEN UPDATE SET
                    nombre=s.nombre, telefono=s.telefono, correo=s.correo,
                    telefono2=s.telefono2, fax=s.fax,
                    codigo_proveedor=s.codigo_proveedor, atiende=s.atiende,
                    direccion1=s.direccion1, direccion2=s.direccion2,
                    ciudad=s.ciudad, estado=s.estado,
                    codigo_postal=s.codigo_postal, pais=s.pais,
                    nombre_alterno=s.nombre_alterno,
                    origen='mba', updated_at=GETDATE()
                WHEN NOT MATCHED BY TARGET THEN INSERT (
                    nombre, rfc, telefono, correo, telefono2, fax,
                    codigo_proveedor, atiende, direccion1, direccion2,
                    ciudad, estado, codigo_postal, pais, nombre_alterno,
                    origen, activo, created_at, updated_at
                ) VALUES (
                    s.nombre, s.rfc, s.telefono, s.correo, s.telefono2, s.fax,
                    s.codigo_proveedor, s.atiende, s.direccion1, s.direccion2,
                    s.ciudad, s.estado, s.codigo_postal, s.pais, s.nombre_alterno,
                    'mba', 1, GETDATE(), GETDATE()
                )
                OUTPUT $action INTO @output;

                SELECT
                    SUM(CASE WHEN accion = 'INSERT' THEN 1 ELSE 0 END) AS insertados,
                    SUM(CASE WHEN accion = 'UPDATE' THEN 1 ELSE 0 END) AS actualizados
                FROM @output;");

            _logger.LogInformation(
                "Sincronización MBA3 completada. Insertados: {Insertados}, Actualizados: {Actualizados}",
                (int)(result.insertados ?? 0),
                (int)(result.actualizados ?? 0));
        }

        private async Task<string> ObtenerJwtAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var body = new StringContent(
                JsonSerializer.Serialize(new { codigo = MBA3_CODIGO, pwd = MBA3_PWD }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(MBA3_BASE + "/login_servicio", body);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("jwt", out var jwtEl))
                throw new InvalidOperationException("MBA3 no devolvió JWT en /login_servicio");

            return jwtEl.GetString() ?? throw new InvalidOperationException("JWT vacío en respuesta de MBA3");
        }

        private static string? Str(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString()?.Trim();
                return string.IsNullOrEmpty(s) ? null : s;
            }
            return null;
        }
    }
}
