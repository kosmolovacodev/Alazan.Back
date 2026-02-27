using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using SistemaAlazan.Models;
using Microsoft.Data.SqlClient;

namespace Alazan.API.Controllers
{
    [ApiController]

    [Route("[controller]")]

    public class CatalogosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public CatalogosController(IDbConnection db) => _db = db;

        // --- MODELOS DE ENTRADA (DTOs) ---
        public class GranoReq {
            public string Nombre { get; set; }
            public bool Requiere_analisis { get; set; }
            public int Sede_id { get; set; }
        }
        public class CalibreReq {
            public string Nombre { get; set; }
            public int? Grano_id { get; set; }
            public int Sede_id { get; set; }
        }
        public class CompradorReq {
            public string Nombre { get; set; }
            public string Telefono { get; set; }
            public int Sede_id { get; set; }
        }
        public class OrigenReq {
            public string Municipio { get; set; }
            public string Estado { get; set; }
            public string Region { get; set; }
            public int Sede_id { get; set; }
        }
        public class BancoReq {
            public string Nombre { get; set; }
            public string Codigo { get; set; }
            public int Sede_id { get; set; }
        }
        public class SedeReq {
            public string Nombre { get; set; }
            public string Ciudad { get; set; }
            public string Estado { get; set; }
            public decimal Tope_diario { get; set; }
        }
        public class SiloCalibreReq {
            public string Nombre { get; set; }
            public int? CalibreId { get; set; }
            public decimal Capacidad_toneladas { get; set; }
            public string Descripcion { get; set; }
        }
        public class SiloPulmonReq {
            public string Nombre { get; set; }
            public decimal Capacidad_toneladas { get; set; }
            public string Descripcion { get; set; }
            public string Tipo { get; set; } // Ej: "ENTRADA", "SALIDA", "TEMPORAL"
        }
        public class AlmacenReq {
            public string Nombre { get; set; }
            public int? Grano_id { get; set; }
            public int Sede_id { get; set; }
        }
        public class ProductorCatalogoReq {
            // Campos originales
            public string Nombre { get; set; }
            public string? Telefono { get; set; }
            public string? Rfc { get; set; }
            public string? Correo { get; set; }
            public string? Tipo_persona { get; set; }
            public int? Banco_id { get; set; }
            public string? Cuenta_clabe { get; set; }
            public string? Atiende { get; set; }
            // MBA3 - Identificadores
            public int? Numero_erp { get; set; }
            public string? Codigo_proveedor { get; set; }
            // MBA3 - Dirección
            public string? Direccion1 { get; set; }
            public string? Direccion2 { get; set; }
            public string? Pais { get; set; }
            public string? Estado { get; set; }
            public string? Ciudad { get; set; }
            public string? Sector { get; set; }
            public string? Codigo_postal { get; set; }
            public string? Numero_exterior { get; set; }
            public string? Numero_interior { get; set; }
            public string? Colonia { get; set; }
            public string? Localidad { get; set; }
            // MBA3 - Contacto extra
            public string? Telefono2 { get; set; }
            public string? Fax { get; set; }
            // MBA3 - Legal
            public string? Nombre_alterno { get; set; }
            public string? Sucursal { get; set; }
            public string? Localizacion { get; set; }
            public string? Regimen_fiscal { get; set; }
            // MBA3 - Comercial
            public int? Termino_pago { get; set; }
            public decimal? Limite_credito { get; set; }
            public decimal? Limite_credito2 { get; set; }
            public string? Codigo_tipo_proveedor { get; set; }
            public string? Codigo_moneda { get; set; }
            public string? Codigo_zona { get; set; }
            public bool? Moneda_unica { get; set; }
            public bool? Proveedor_global { get; set; }
            public bool? Usar_nombre_alterno { get; set; }
            public bool? Relacionada { get; set; }
            // MBA3 - Fiscal
            public string? Retenciones { get; set; }
            public string? Impuestos { get; set; }
            public string? Grupo_impuestos { get; set; }
            // MBA3 - Contabilidad
            public string? Cuenta_contable_pagar { get; set; }
            public string? Cuenta_contable_anticipo { get; set; }
            // MBA3 - Bancario extra
            public string? Cuenta_bancaria2 { get; set; }
            public string? Aba_swift { get; set; }
            public string? Beneficiario { get; set; }
            public string? Codigo_transferencia { get; set; }
            public string? Codigo_transaccion { get; set; }
            public string? Definible_transferencia1 { get; set; }
            public string? Definible_transferencia2 { get; set; }
            public string? Definible_transferencia3 { get; set; }
            // MBA3 - Notas y metadata
            public string? Memo { get; set; }
            public DateTime? Fecha_creacion_erp { get; set; }
        }
        public class GenericUpdate { public bool Activo { get; set; } }

        // --- 1. GRANOS (Filtrado por sede) ---
        [HttpGet("granos")]
        public async Task<IActionResult> GetGranos([FromQuery] int sedeId)
        {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT id, nombre, requiere_analisis, activo
                          FROM dbo.granos_catalogo
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("granos")]
        public async Task<IActionResult> AddGrano([FromBody] GranoReq item, [FromQuery] int sedeId)
        {
            const string sql = "INSERT INTO dbo.granos_catalogo (nombre, requiere_analisis, activo, sede_id) VALUES (@Nombre, @Req, 1, @SedeId)";
            await _db.ExecuteAsync(sql, new { item.Nombre, Req = item.Requiere_analisis ? 1 : 0, SedeId = sedeId });
            return Ok();
        }

        // --- 2. CALIBRES (Filtrado por sede y opcionalmente por grano) ---
        [HttpGet("calibres")]
        public async Task<IActionResult> GetCalibres([FromQuery] int sedeId, [FromQuery] int granoId = 0)
        {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT
                            c.id,
                            c.calibre_mm as nombre,
                            c.grano_id as granoId,
                            g.nombre as granoNombre,
                            c.activo
                          FROM dbo.calibres_catalogo c
                          LEFT JOIN dbo.granos_catalogo g ON c.grano_id = g.id
                          WHERE (@sedeId = 0 OR c.sede_id = @sedeId OR c.sede_id = 0 OR c.sede_id IS NULL)";

            if (granoId > 0)
            {
                sql += " AND c.grano_id = @granoId";
            }

            return Ok(await _db.QueryAsync(sql, new { sedeId, granoId }));
        }

        [HttpPost("calibres")]
        public async Task<IActionResult> AddCalibre([FromBody] CalibreReq item, [FromQuery] int sedeId)
        {
            await _db.ExecuteAsync(
                "INSERT INTO dbo.calibres_catalogo (calibre_mm, grano_id, activo, sede_id) VALUES (@Nombre, @GranoId, 1, @SedeId)",
                new { item.Nombre, GranoId = item.Grano_id, SedeId = sedeId });
            return Ok();
        }

        // --- 3. COMPRADORES (Filtrado por sede) ---
        [HttpGet("compradores")]
        public async Task<IActionResult> GetCompradores([FromQuery] int sedeId) {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT id, nombre, telefono, activo
                          FROM dbo.compradores_catalogo
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("compradores")]
        public async Task<IActionResult> AddComprador([FromBody] CompradorReq item) {
            await _db.ExecuteAsync("INSERT INTO dbo.compradores_catalogo (nombre, telefono, activo, sede_id) VALUES (@Nombre, @Telefono, 1, @Sede_id)", item);
            return Ok();
        }

        // --- PRODUCTORES (Filtrado por sede, con paginación y búsqueda) ---
        [HttpGet("productores")]
        public async Task<IActionResult> GetProductores(
            [FromQuery] int sedeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string search = "")
        {
            const string sedeFilter = "(@sedeId = 0 OR p.sede_id = @sedeId OR p.sede_id = 0 OR p.sede_id IS NULL)";
            var searchFilter = string.IsNullOrWhiteSpace(search)
                ? ""
                : "AND (p.nombre LIKE @searchParam OR p.rfc LIKE @searchParam OR p.correo LIKE @searchParam OR p.nombre_alterno LIKE @searchParam)";
            var searchParam = $"%{search}%";
            var offset = (page - 1) * pageSize;

            var countSql = $"SELECT COUNT(*) FROM dbo.productores p WHERE {sedeFilter} {searchFilter}";

            var dataSql = $@"SELECT
                              p.id, p.nombre, p.telefono, p.rfc, p.correo,
                              p.tipo_persona, p.banco_id, p.cuenta_clabe, p.atiende, p.activo,
                              b.nombre_banco AS nombreBanco,
                              p.numero_erp, p.codigo_proveedor,
                              p.direccion1, p.direccion2, p.pais, p.estado, p.ciudad,
                              p.sector, p.codigo_postal, p.numero_exterior, p.numero_interior,
                              p.colonia, p.localidad,
                              p.telefono2, p.fax,
                              p.nombre_alterno, p.sucursal, p.localizacion, p.regimen_fiscal,
                              p.termino_pago, p.limite_credito, p.limite_credito2,
                              p.codigo_tipo_proveedor, p.codigo_moneda, p.codigo_zona,
                              p.moneda_unica, p.proveedor_global, p.usar_nombre_alterno, p.relacionada,
                              p.retenciones, p.impuestos, p.grupo_impuestos,
                              p.cuenta_contable_pagar, p.cuenta_contable_anticipo,
                              p.cuenta_bancaria2, p.aba_swift, p.beneficiario,
                              p.codigo_transferencia, p.codigo_transaccion,
                              p.definible_transferencia1, p.definible_transferencia2, p.definible_transferencia3,
                              p.memo, p.fecha_creacion_erp
                          FROM dbo.productores p
                          LEFT JOIN dbo.bancos_catalogo b ON p.banco_id = b.id
                          WHERE {sedeFilter} {searchFilter}
                          ORDER BY p.nombre
                          OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            try
            {
                var total = await _db.QuerySingleAsync<int>(countSql, new { sedeId, searchParam });
                var items = await _db.QueryAsync(dataSql, new { sedeId, searchParam, offset, pageSize });
                return Ok(new { items, total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error productores: {ex.Message}");
            }
        }

        [HttpPost("productores")]
        public async Task<IActionResult> AddProductor([FromBody] ProductorCatalogoReq item, [FromQuery] int sedeId)
        {
            try
            {
                var sql = @"INSERT INTO dbo.productores (
                                nombre, telefono, rfc, correo, tipo_persona, banco_id, cuenta_clabe, atiende,
                                activo, created_at, updated_at, sede_id,
                                numero_erp, codigo_proveedor,
                                direccion1, direccion2, pais, estado, ciudad, sector, codigo_postal,
                                numero_exterior, numero_interior, colonia, localidad,
                                telefono2, fax,
                                nombre_alterno, sucursal, localizacion, regimen_fiscal,
                                termino_pago, limite_credito, limite_credito2,
                                codigo_tipo_proveedor, codigo_moneda, codigo_zona,
                                moneda_unica, proveedor_global, usar_nombre_alterno, relacionada,
                                retenciones, impuestos, grupo_impuestos,
                                cuenta_contable_pagar, cuenta_contable_anticipo,
                                cuenta_bancaria2, aba_swift, beneficiario,
                                codigo_transferencia, codigo_transaccion,
                                definible_transferencia1, definible_transferencia2, definible_transferencia3,
                                memo, fecha_creacion_erp
                            ) VALUES (
                                @Nombre, @Telefono, @Rfc, @Correo, @Tipo_persona, @Banco_id, @Cuenta_clabe, @Atiende,
                                1, GETDATE(), GETDATE(), @SedeId,
                                @Numero_erp, @Codigo_proveedor,
                                @Direccion1, @Direccion2, @Pais, @Estado, @Ciudad, @Sector, @Codigo_postal,
                                @Numero_exterior, @Numero_interior, @Colonia, @Localidad,
                                @Telefono2, @Fax,
                                @Nombre_alterno, @Sucursal, @Localizacion, @Regimen_fiscal,
                                @Termino_pago, @Limite_credito, @Limite_credito2,
                                @Codigo_tipo_proveedor, @Codigo_moneda, @Codigo_zona,
                                @Moneda_unica, @Proveedor_global, @Usar_nombre_alterno, @Relacionada,
                                @Retenciones, @Impuestos, @Grupo_impuestos,
                                @Cuenta_contable_pagar, @Cuenta_contable_anticipo,
                                @Cuenta_bancaria2, @Aba_swift, @Beneficiario,
                                @Codigo_transferencia, @Codigo_transaccion,
                                @Definible_transferencia1, @Definible_transferencia2, @Definible_transferencia3,
                                @Memo, @Fecha_creacion_erp
                            );
                            SELECT CAST(SCOPE_IDENTITY() as int);";
                var id = await _db.QuerySingleAsync<int>(sql, new {
                    item.Nombre, item.Telefono, item.Rfc, item.Correo,
                    item.Tipo_persona, item.Banco_id, item.Cuenta_clabe, item.Atiende,
                    SedeId = sedeId,
                    item.Numero_erp, item.Codigo_proveedor,
                    item.Direccion1, item.Direccion2, item.Pais, item.Estado, item.Ciudad,
                    item.Sector, item.Codigo_postal, item.Numero_exterior, item.Numero_interior,
                    item.Colonia, item.Localidad,
                    item.Telefono2, item.Fax,
                    item.Nombre_alterno, item.Sucursal, item.Localizacion, item.Regimen_fiscal,
                    item.Termino_pago, item.Limite_credito, item.Limite_credito2,
                    item.Codigo_tipo_proveedor, item.Codigo_moneda, item.Codigo_zona,
                    item.Moneda_unica, item.Proveedor_global, item.Usar_nombre_alterno, item.Relacionada,
                    item.Retenciones, item.Impuestos, item.Grupo_impuestos,
                    item.Cuenta_contable_pagar, item.Cuenta_contable_anticipo,
                    item.Cuenta_bancaria2, item.Aba_swift, item.Beneficiario,
                    item.Codigo_transferencia, item.Codigo_transaccion,
                    item.Definible_transferencia1, item.Definible_transferencia2, item.Definible_transferencia3,
                    item.Memo, item.Fecha_creacion_erp
                });
                return Ok(new { id, nombre = item.Nombre });
            }
            catch (Exception ex) {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("productores/importar")]
        public async Task<IActionResult> ImportarProductores([FromBody] List<ProductorCatalogoReq> items, [FromQuery] int sedeId)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { message = "No se recibieron registros" });

            var conn = (SqlConnection)_db;
            if (conn.State != ConnectionState.Open) conn.Open();

            try
            {
                // 1. Tabla temporal con la misma estructura que productores
                await conn.ExecuteAsync(@"
                    CREATE TABLE #tmp_prod (
                        nombre                   NVARCHAR(300)  NULL,
                        telefono                 NVARCHAR(50)   NULL,
                        rfc                      NVARCHAR(20)   NULL,
                        correo                   NVARCHAR(200)  NULL,
                        tipo_persona             NVARCHAR(20)   NULL,
                        banco_id                 INT            NULL,
                        cuenta_clabe             NVARCHAR(50)   NULL,
                        atiende                  NVARCHAR(200)  NULL,
                        numero_erp               INT            NULL,
                        codigo_proveedor         NVARCHAR(50)   NULL,
                        direccion1               NVARCHAR(500)  NULL,
                        direccion2               NVARCHAR(500)  NULL,
                        pais                     NVARCHAR(100)  NULL,
                        estado                   NVARCHAR(100)  NULL,
                        ciudad                   NVARCHAR(100)  NULL,
                        sector                   NVARCHAR(100)  NULL,
                        codigo_postal            NVARCHAR(10)   NULL,
                        numero_exterior          NVARCHAR(20)   NULL,
                        numero_interior          NVARCHAR(20)   NULL,
                        colonia                  NVARCHAR(200)  NULL,
                        localidad                NVARCHAR(200)  NULL,
                        telefono2                NVARCHAR(50)   NULL,
                        fax                      NVARCHAR(50)   NULL,
                        nombre_alterno           NVARCHAR(300)  NULL,
                        sucursal                 NVARCHAR(100)  NULL,
                        localizacion             NVARCHAR(200)  NULL,
                        regimen_fiscal           NVARCHAR(50)   NULL,
                        termino_pago             INT            NULL,
                        limite_credito           DECIMAL(18,2)  NULL,
                        limite_credito2          DECIMAL(18,2)  NULL,
                        codigo_tipo_proveedor    NVARCHAR(20)   NULL,
                        codigo_moneda            NVARCHAR(10)   NULL,
                        codigo_zona              NVARCHAR(20)   NULL,
                        moneda_unica             BIT            NULL,
                        proveedor_global         BIT            NULL,
                        usar_nombre_alterno      BIT            NULL,
                        relacionada              BIT            NULL,
                        retenciones              NVARCHAR(100)  NULL,
                        impuestos                NVARCHAR(100)  NULL,
                        grupo_impuestos          NVARCHAR(100)  NULL,
                        cuenta_contable_pagar    NVARCHAR(50)   NULL,
                        cuenta_contable_anticipo NVARCHAR(50)   NULL,
                        cuenta_bancaria2         NVARCHAR(50)   NULL,
                        aba_swift                NVARCHAR(50)   NULL,
                        beneficiario             NVARCHAR(300)  NULL,
                        codigo_transferencia     NVARCHAR(50)   NULL,
                        codigo_transaccion       NVARCHAR(50)   NULL,
                        definible_transferencia1 NVARCHAR(100)  NULL,
                        definible_transferencia2 NVARCHAR(100)  NULL,
                        definible_transferencia3 NVARCHAR(100)  NULL,
                        memo                     NVARCHAR(MAX)  NULL,
                        fecha_creacion_erp       DATE           NULL
                    )");

                // 2. Construir DataTable en memoria
                var dt = new DataTable();
                dt.Columns.Add("nombre");
                dt.Columns.Add("telefono");
                dt.Columns.Add("rfc");
                dt.Columns.Add("correo");
                dt.Columns.Add("tipo_persona");
                dt.Columns.Add("banco_id",        typeof(int));
                dt.Columns.Add("cuenta_clabe");
                dt.Columns.Add("atiende");
                dt.Columns.Add("numero_erp",      typeof(int));
                dt.Columns.Add("codigo_proveedor");
                dt.Columns.Add("direccion1");
                dt.Columns.Add("direccion2");
                dt.Columns.Add("pais");
                dt.Columns.Add("estado");
                dt.Columns.Add("ciudad");
                dt.Columns.Add("sector");
                dt.Columns.Add("codigo_postal");
                dt.Columns.Add("numero_exterior");
                dt.Columns.Add("numero_interior");
                dt.Columns.Add("colonia");
                dt.Columns.Add("localidad");
                dt.Columns.Add("telefono2");
                dt.Columns.Add("fax");
                dt.Columns.Add("nombre_alterno");
                dt.Columns.Add("sucursal");
                dt.Columns.Add("localizacion");
                dt.Columns.Add("regimen_fiscal");
                dt.Columns.Add("termino_pago",    typeof(int));
                dt.Columns.Add("limite_credito",  typeof(decimal));
                dt.Columns.Add("limite_credito2", typeof(decimal));
                dt.Columns.Add("codigo_tipo_proveedor");
                dt.Columns.Add("codigo_moneda");
                dt.Columns.Add("codigo_zona");
                dt.Columns.Add("moneda_unica",         typeof(bool));
                dt.Columns.Add("proveedor_global",     typeof(bool));
                dt.Columns.Add("usar_nombre_alterno",  typeof(bool));
                dt.Columns.Add("relacionada",          typeof(bool));
                dt.Columns.Add("retenciones");
                dt.Columns.Add("impuestos");
                dt.Columns.Add("grupo_impuestos");
                dt.Columns.Add("cuenta_contable_pagar");
                dt.Columns.Add("cuenta_contable_anticipo");
                dt.Columns.Add("cuenta_bancaria2");
                dt.Columns.Add("aba_swift");
                dt.Columns.Add("beneficiario");
                dt.Columns.Add("codigo_transferencia");
                dt.Columns.Add("codigo_transaccion");
                dt.Columns.Add("definible_transferencia1");
                dt.Columns.Add("definible_transferencia2");
                dt.Columns.Add("definible_transferencia3");
                dt.Columns.Add("memo");
                dt.Columns.Add("fecha_creacion_erp", typeof(DateTime));
                foreach (DataColumn c in dt.Columns) c.AllowDBNull = true;

                static object Db(object? v) => v ?? DBNull.Value;

                foreach (var item in items)
                {
                    var rfcLimpio = string.IsNullOrWhiteSpace(item.Rfc) ? null : item.Rfc.Trim();
                    dt.Rows.Add(
                        item.Nombre, item.Telefono, rfcLimpio, item.Correo, item.Tipo_persona,
                        Db(item.Banco_id), item.Cuenta_clabe, item.Atiende,
                        Db(item.Numero_erp), item.Codigo_proveedor,
                        item.Direccion1, item.Direccion2, item.Pais, item.Estado, item.Ciudad,
                        item.Sector, item.Codigo_postal, item.Numero_exterior, item.Numero_interior,
                        item.Colonia, item.Localidad,
                        item.Telefono2, item.Fax,
                        item.Nombre_alterno, item.Sucursal, item.Localizacion, item.Regimen_fiscal,
                        Db(item.Termino_pago), Db(item.Limite_credito), Db(item.Limite_credito2),
                        item.Codigo_tipo_proveedor, item.Codigo_moneda, item.Codigo_zona,
                        Db(item.Moneda_unica), Db(item.Proveedor_global), Db(item.Usar_nombre_alterno), Db(item.Relacionada),
                        item.Retenciones, item.Impuestos, item.Grupo_impuestos,
                        item.Cuenta_contable_pagar, item.Cuenta_contable_anticipo,
                        item.Cuenta_bancaria2, item.Aba_swift, item.Beneficiario,
                        item.Codigo_transferencia, item.Codigo_transaccion,
                        item.Definible_transferencia1, item.Definible_transferencia2, item.Definible_transferencia3,
                        item.Memo, Db(item.Fecha_creacion_erp)
                    );
                }

                // 3. Bulk copy a la tabla temporal (una sola operación de red)
                using var bulk = new SqlBulkCopy(conn) { DestinationTableName = "#tmp_prod", BulkCopyTimeout = 120 };
                await bulk.WriteToServerAsync(dt);

                // 4. MERGE deduplicado por RFC + INSERT separado para filas sin RFC
                var result = await conn.QueryFirstAsync<dynamic>(@"
                    DECLARE @output TABLE (accion NVARCHAR(10));

                    -- MERGE solo para filas con RFC, deduplicadas (primera ocurrencia por RFC)
                    MERGE dbo.productores AS t
                    USING (
                        SELECT * FROM (
                            SELECT *, ROW_NUMBER() OVER (PARTITION BY rfc ORDER BY (SELECT NULL)) AS rn
                            FROM #tmp_prod
                            WHERE rfc IS NOT NULL AND rfc != ''
                        ) x WHERE rn = 1
                    ) AS s ON t.rfc = s.rfc
                    WHEN MATCHED THEN UPDATE SET
                        nombre=s.nombre, telefono=s.telefono, correo=s.correo,
                        tipo_persona=s.tipo_persona, banco_id=s.banco_id,
                        cuenta_clabe=s.cuenta_clabe, atiende=s.atiende,
                        updated_at=GETDATE(),
                        numero_erp=s.numero_erp, codigo_proveedor=s.codigo_proveedor,
                        direccion1=s.direccion1, direccion2=s.direccion2, pais=s.pais,
                        estado=s.estado, ciudad=s.ciudad, sector=s.sector,
                        codigo_postal=s.codigo_postal, numero_exterior=s.numero_exterior,
                        numero_interior=s.numero_interior, colonia=s.colonia, localidad=s.localidad,
                        telefono2=s.telefono2, fax=s.fax,
                        nombre_alterno=s.nombre_alterno, sucursal=s.sucursal,
                        localizacion=s.localizacion, regimen_fiscal=s.regimen_fiscal,
                        termino_pago=s.termino_pago, limite_credito=s.limite_credito,
                        limite_credito2=s.limite_credito2,
                        codigo_tipo_proveedor=s.codigo_tipo_proveedor,
                        codigo_moneda=s.codigo_moneda, codigo_zona=s.codigo_zona,
                        moneda_unica=s.moneda_unica, proveedor_global=s.proveedor_global,
                        usar_nombre_alterno=s.usar_nombre_alterno, relacionada=s.relacionada,
                        retenciones=s.retenciones, impuestos=s.impuestos,
                        grupo_impuestos=s.grupo_impuestos,
                        cuenta_contable_pagar=s.cuenta_contable_pagar,
                        cuenta_contable_anticipo=s.cuenta_contable_anticipo,
                        cuenta_bancaria2=s.cuenta_bancaria2, aba_swift=s.aba_swift,
                        beneficiario=s.beneficiario, codigo_transferencia=s.codigo_transferencia,
                        codigo_transaccion=s.codigo_transaccion,
                        definible_transferencia1=s.definible_transferencia1,
                        definible_transferencia2=s.definible_transferencia2,
                        definible_transferencia3=s.definible_transferencia3,
                        memo=s.memo, fecha_creacion_erp=s.fecha_creacion_erp
                    WHEN NOT MATCHED THEN INSERT (
                        nombre, telefono, rfc, correo, tipo_persona, banco_id, cuenta_clabe, atiende,
                        activo, created_at, updated_at, sede_id,
                        numero_erp, codigo_proveedor,
                        direccion1, direccion2, pais, estado, ciudad, sector, codigo_postal,
                        numero_exterior, numero_interior, colonia, localidad,
                        telefono2, fax,
                        nombre_alterno, sucursal, localizacion, regimen_fiscal,
                        termino_pago, limite_credito, limite_credito2,
                        codigo_tipo_proveedor, codigo_moneda, codigo_zona,
                        moneda_unica, proveedor_global, usar_nombre_alterno, relacionada,
                        retenciones, impuestos, grupo_impuestos,
                        cuenta_contable_pagar, cuenta_contable_anticipo,
                        cuenta_bancaria2, aba_swift, beneficiario,
                        codigo_transferencia, codigo_transaccion,
                        definible_transferencia1, definible_transferencia2, definible_transferencia3,
                        memo, fecha_creacion_erp
                    ) VALUES (
                        s.nombre, s.telefono, s.rfc, s.correo, s.tipo_persona, s.banco_id, s.cuenta_clabe, s.atiende,
                        1, GETDATE(), GETDATE(), @sedeId,
                        s.numero_erp, s.codigo_proveedor,
                        s.direccion1, s.direccion2, s.pais, s.estado, s.ciudad, s.sector, s.codigo_postal,
                        s.numero_exterior, s.numero_interior, s.colonia, s.localidad,
                        s.telefono2, s.fax,
                        s.nombre_alterno, s.sucursal, s.localizacion, s.regimen_fiscal,
                        s.termino_pago, s.limite_credito, s.limite_credito2,
                        s.codigo_tipo_proveedor, s.codigo_moneda, s.codigo_zona,
                        s.moneda_unica, s.proveedor_global, s.usar_nombre_alterno, s.relacionada,
                        s.retenciones, s.impuestos, s.grupo_impuestos,
                        s.cuenta_contable_pagar, s.cuenta_contable_anticipo,
                        s.cuenta_bancaria2, s.aba_swift, s.beneficiario,
                        s.codigo_transferencia, s.codigo_transaccion,
                        s.definible_transferencia1, s.definible_transferencia2, s.definible_transferencia3,
                        s.memo, s.fecha_creacion_erp
                    )
                    OUTPUT $action INTO @output;

                    -- INSERT directo para filas sin RFC (sin riesgo de duplicados en MERGE)
                    INSERT INTO dbo.productores (
                        nombre, telefono, rfc, correo, tipo_persona, banco_id, cuenta_clabe, atiende,
                        activo, created_at, updated_at, sede_id,
                        numero_erp, codigo_proveedor,
                        direccion1, direccion2, pais, estado, ciudad, sector, codigo_postal,
                        numero_exterior, numero_interior, colonia, localidad,
                        telefono2, fax,
                        nombre_alterno, sucursal, localizacion, regimen_fiscal,
                        termino_pago, limite_credito, limite_credito2,
                        codigo_tipo_proveedor, codigo_moneda, codigo_zona,
                        moneda_unica, proveedor_global, usar_nombre_alterno, relacionada,
                        retenciones, impuestos, grupo_impuestos,
                        cuenta_contable_pagar, cuenta_contable_anticipo,
                        cuenta_bancaria2, aba_swift, beneficiario,
                        codigo_transferencia, codigo_transaccion,
                        definible_transferencia1, definible_transferencia2, definible_transferencia3,
                        memo, fecha_creacion_erp
                    )
                    SELECT
                        nombre, telefono, NULL, correo, tipo_persona, banco_id, cuenta_clabe, atiende,
                        1, GETDATE(), GETDATE(), @sedeId,
                        numero_erp, codigo_proveedor,
                        direccion1, direccion2, pais, estado, ciudad, sector, codigo_postal,
                        numero_exterior, numero_interior, colonia, localidad,
                        telefono2, fax,
                        nombre_alterno, sucursal, localizacion, regimen_fiscal,
                        termino_pago, limite_credito, limite_credito2,
                        codigo_tipo_proveedor, codigo_moneda, codigo_zona,
                        moneda_unica, proveedor_global, usar_nombre_alterno, relacionada,
                        retenciones, impuestos, grupo_impuestos,
                        cuenta_contable_pagar, cuenta_contable_anticipo,
                        cuenta_bancaria2, aba_swift, beneficiario,
                        codigo_transferencia, codigo_transaccion,
                        definible_transferencia1, definible_transferencia2, definible_transferencia3,
                        memo, fecha_creacion_erp
                    FROM #tmp_prod
                    WHERE rfc IS NULL OR rfc = '';

                    SELECT
                        SUM(CASE WHEN accion = 'INSERT' THEN 1 ELSE 0 END) +
                            (SELECT COUNT(*) FROM #tmp_prod WHERE rfc IS NULL OR rfc = '') AS insertados,
                        SUM(CASE WHEN accion = 'UPDATE' THEN 1 ELSE 0 END) AS actualizados
                    FROM @output;", new { sedeId });

                return Ok(new { insertados = (int)(result.insertados ?? 0), actualizados = (int)(result.actualizados ?? 0) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error en importación: {ex.Message}");
            }
        }

        // --- 4. ORIGENES (Filtrado por sede) ---
        [HttpGet("origenes")]
        public async Task<IActionResult> GetOrigenes([FromQuery] int sedeId) {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT id, municipio, estado, region, activo
                          FROM dbo.origenes_catalogo
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("origenes")]
        public async Task<IActionResult> AddOrigen([FromBody] OrigenReq item) {
            await _db.ExecuteAsync("INSERT INTO dbo.origenes_catalogo (municipio, estado, region, activo, sede_id) VALUES (@Municipio, @Estado, @Region, 1, @Sede_id)", item);
            return Ok();
        }

        // --- 5. BANCOS (Filtrado por sede) ---
        [HttpGet("bancos")]
        public async Task<IActionResult> GetBancos([FromQuery] int sedeId)
        {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT id, nombre_banco as nombre, codigo_banco as codigo, activo
                          FROM dbo.bancos_catalogo
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("bancos")]
        public async Task<IActionResult> AddBanco([FromBody] BancoReq item, [FromQuery] int sedeId)
        {
            await _db.ExecuteAsync("INSERT INTO dbo.bancos_catalogo (nombre_banco, codigo_banco, activo, sede_id) VALUES (@Nombre, @Codigo, 1, @SedeId)", new { item.Nombre, item.Codigo, SedeId = sedeId });
            return Ok();
        }

        // --- 6. SEDES (Global) ---
        [HttpGet("sedes")]
        public async Task<IActionResult> GetSedes() 
        {
            return Ok(await _db.QueryAsync(@"
                SELECT 
                    id, 
                    nombre_sede as nombre, 
                    ciudad, 
                    estado, 
                    tope_diario, 
                    activo 
                FROM dbo.sedes_catalogo"));
        }

        [HttpPost("sedes")]
        public async Task<IActionResult> AddSede([FromBody] SedeReq item) 
        {
            try 
            {
                const string sql = @"
                    INSERT INTO dbo.sedes_catalogo 
                    (nombre_sede, ciudad, estado, tope_diario, activo, created_at) 
                    VALUES 
                    (@Nombre, @Ciudad, @Estado, @Tope_diario, 1, GETDATE())";
                    
                await _db.ExecuteAsync(sql, item);
                return Ok();
            }
            catch (Exception ex) 
            {
                return BadRequest(new { error = "Error en BD", detalle = ex.Message });
            }
        }

        // --- 7. SILOS CALIBRE (Silos asignados por calibre) ---
        [HttpGet("silos-calibre")]
        public async Task<IActionResult> GetSilosCalibre([FromQuery] int sedeId)
        {
            string sql = @"SELECT
                            sc.id,
                            sc.nombre,
                            sc.calibre_id as calibreId,
                            c.calibre_mm as calibreNombre,
                            sc.capacidad_toneladas as capacidadToneladas,
                            sc.descripcion,
                            sc.activo
                          FROM dbo.silos_calibre_catalogo sc
                          LEFT JOIN dbo.calibres_catalogo c ON sc.calibre_id = c.id
                          WHERE @sedeId = 0 OR sc.sede_id = @sedeId OR sc.sede_id = 0 OR sc.sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("silos-calibre")]
        public async Task<IActionResult> AddSiloCalibre([FromBody] SiloCalibreReq item, [FromQuery] int sedeId)
        {
            const string sql = @"INSERT INTO dbo.silos_calibre_catalogo
                                (nombre, calibre_id, capacidad_toneladas, descripcion, activo, sede_id, created_at)
                                VALUES (@Nombre, @CalibreId, @CapacidadToneladas, @Descripcion, 1, @SedeId, GETDATE())";
            await _db.ExecuteAsync(sql, new {
                item.Nombre,
                item.CalibreId,
                CapacidadToneladas = item.Capacidad_toneladas,
                item.Descripcion,
                SedeId = sedeId
            });
            return Ok();
        }

        // --- 8. SILOS PULMÓN (Silos temporales/de tránsito) ---
        [HttpGet("silos-pulmon")]
        public async Task<IActionResult> GetSilosPulmon([FromQuery] int sedeId)
        {
            string sql = @"SELECT
                            id,
                            nombre,
                            capacidad_toneladas as capacidadToneladas,
                            descripcion,
                            tipo,
                            activo
                          FROM dbo.silos_pulmon_catalogo
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("silos-pulmon")]
        public async Task<IActionResult> AddSiloPulmon([FromBody] SiloPulmonReq item, [FromQuery] int sedeId)
        {
            const string sql = @"INSERT INTO dbo.silos_pulmon_catalogo
                                (nombre, capacidad_toneladas, descripcion, tipo, activo, sede_id, created_at)
                                VALUES (@Nombre, @CapacidadToneladas, @Descripcion, @Tipo, 1, @SedeId, GETDATE())";
            await _db.ExecuteAsync(sql, new {
                item.Nombre,
                CapacidadToneladas = item.Capacidad_toneladas,
                item.Descripcion,
                item.Tipo,
                SedeId = sedeId
            });
            return Ok();
        }

        // --- 9. ALMACENES / BODEGAS (Filtrado por sede, con tipo de grano) ---
        [HttpGet("bodegas")]
        public async Task<IActionResult> GetBodegas([FromQuery] int sede_id)
        {
            string sql = @"SELECT
                            ca.id,
                            ca.nombre_almacen AS nombre,
                            ca.grano_id AS granoId,
                            g.nombre AS granoNombre,
                            ca.activo
                          FROM dbo.catalogo_almacenes ca
                          LEFT JOIN dbo.granos_catalogo g ON ca.grano_id = g.id
                          WHERE @sede_id = 0 OR ca.sede_id = @sede_id OR ca.sede_id = 0 OR ca.sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sede_id }));
        }

        [HttpPost("bodegas")]
        public async Task<IActionResult> AddBodega([FromBody] AlmacenReq item)
        {
            const string sql = @"INSERT INTO dbo.catalogo_almacenes
                                (nombre_almacen, grano_id, activo, sede_id)
                                VALUES (@Nombre, @GranoId, 1, @SedeId)";
            await _db.ExecuteAsync(sql, new { item.Nombre, GranoId = item.Grano_id, SedeId = item.Sede_id });
            return Ok();
        }

        // --- EDICIÓN COMPLETA DE CATÁLOGOS ---
        public class EditarCatalogoReq {
            public string Nombre { get; set; }
            public bool Requiere_analisis { get; set; }
            public string Telefono { get; set; }
            public string Municipio { get; set; }
            public string Estado { get; set; }
            public string Region { get; set; }
            public string Codigo { get; set; }
            public string Ciudad { get; set; }
            public decimal Tope_diario { get; set; }
            public int? Calibre_id { get; set; }
            public decimal Capacidad_toneladas { get; set; }
            public string Descripcion { get; set; }
            public string Tipo { get; set; }
            public int? Grano_id { get; set; }
            public int Sede_id { get; set; }
            // Campos de Productores
            public string? Rfc { get; set; }
            public string? Correo { get; set; }
            public string? Tipo_persona { get; set; }
            public int? Banco_id { get; set; }
            public string? Cuenta_clabe { get; set; }
            public string? Atiende { get; set; }
        }

        [HttpPut("editar/{catalogo}/{id}")]
        public async Task<IActionResult> EditarItem(string catalogo, string id, [FromQuery] int sedeId, [FromBody] EditarCatalogoReq item)
        {
            try
            {
                string sql = catalogo switch
                {
                    "granos" => "UPDATE dbo.granos_catalogo SET nombre = @Nombre, requiere_analisis = @Requiere_analisis, updated_at = GETDATE() WHERE id = @Id",
                    "calibres" => "UPDATE dbo.calibres_catalogo SET calibre_mm = @Nombre, grano_id = @Grano_id, updated_at = GETDATE() WHERE id = @Id",
                    "compradores" => "UPDATE dbo.compradores_catalogo SET nombre = @Nombre, telefono = @Telefono, updated_at = GETDATE() WHERE id = @Id",
                    "origenes" => "UPDATE dbo.origenes_catalogo SET municipio = @Municipio, estado = @Estado, region = @Region, updated_at = GETDATE() WHERE id = @Id",
                    "bancos" => "UPDATE dbo.bancos_catalogo SET nombre_banco = @Nombre, codigo_banco = @Codigo, updated_at = GETDATE() WHERE id = @Id",
                    "sedes" => "UPDATE dbo.sedes_catalogo SET nombre_sede = @Nombre, ciudad = @Ciudad, estado = @Estado, tope_diario = @Tope_diario WHERE id = @Id",
                    "silos-calibre" => "UPDATE dbo.silos_calibre_catalogo SET nombre = @Nombre, calibre_id = @Calibre_id, capacidad_toneladas = @Capacidad_toneladas, descripcion = @Descripcion, updated_at = GETDATE() WHERE id = @Id",
                    "silos-pulmon" => "UPDATE dbo.silos_pulmon_catalogo SET nombre = @Nombre, capacidad_toneladas = @Capacidad_toneladas, descripcion = @Descripcion, tipo = @Tipo, updated_at = GETDATE() WHERE id = @Id",
                    "bodegas" => "UPDATE dbo.catalogo_almacenes SET nombre_almacen = @Nombre, grano_id = @Grano_id, updated_at = GETDATE() WHERE id = @Id",
                    "productores" => @"UPDATE dbo.productores SET nombre = @Nombre, telefono = @Telefono, rfc = @Rfc,
                                        correo = @Correo, tipo_persona = @Tipo_persona, banco_id = @Banco_id,
                                        cuenta_clabe = @Cuenta_clabe, atiende = @Atiende, updated_at = GETDATE() WHERE id = @Id",
                    _ => null
                };

                if (sql == null) return BadRequest("Catálogo no válido");

                // Preparar el parámetro ID con el tipo correcto
                object parameters;
                if (catalogo == "compradores" && Guid.TryParse(id, out var guid))
                {
                    parameters = new {
                        Id = guid,
                        item.Nombre, item.Requiere_analisis, item.Telefono, item.Municipio,
                        item.Estado, item.Region, item.Codigo, item.Ciudad, item.Tope_diario,
                        item.Calibre_id, item.Capacidad_toneladas, item.Descripcion, item.Tipo, item.Grano_id,
                        item.Rfc, item.Correo, item.Tipo_persona, item.Banco_id, item.Cuenta_clabe, item.Atiende
                    };
                }
                else
                {
                    int.TryParse(id, out var intId);
                    parameters = new {
                        Id = intId,
                        item.Nombre, item.Requiere_analisis, item.Telefono, item.Municipio,
                        item.Estado, item.Region, item.Codigo, item.Ciudad, item.Tope_diario,
                        item.Calibre_id, item.Capacidad_toneladas, item.Descripcion, item.Tipo, item.Grano_id,
                        item.Rfc, item.Correo, item.Tipo_persona, item.Banco_id, item.Cuenta_clabe, item.Atiende
                    };
                }

                await _db.ExecuteAsync(sql, parameters);
                return Ok(new { message = "Actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
            }
        }

        // --- MÉTODOS GENÉRICOS (UPDATE ESTADO Y DELETE) ---

        // Catálogos cuyo ID es uniqueidentifier (GUID)
        private static readonly string[] CatalogosConGuid = { "compradores" };

        private static string GetTabla(string catalogo) => catalogo switch {
            "granos" => "dbo.granos_catalogo",
            "calibres" => "dbo.calibres_catalogo",
            "compradores" => "dbo.compradores_catalogo",
            "origenes" => "dbo.origenes_catalogo",
            "bancos" => "dbo.bancos_catalogo",
            "sedes" => "dbo.sedes_catalogo",
            "productores" => "dbo.productores",
            "silos-calibre" => "dbo.silos_calibre_catalogo",
            "silos-pulmon" => "dbo.silos_pulmon_catalogo",
            "bodegas" => "dbo.catalogo_almacenes",
            _ => null
        };

        private object GetIdParam(string catalogo, string id) {
            if (CatalogosConGuid.Contains(catalogo) && Guid.TryParse(id, out var guid))
                return new { id = guid };
            if (int.TryParse(id, out var intId))
                return new { id = intId };
            return new { id };
        }

        [HttpPut("{catalogo}/{id}")]
        public async Task<IActionResult> UpdateEstado(string catalogo, string id, [FromQuery] int sedeId, [FromBody] GenericUpdate item) {
            var catalogosConSede = new[] { "granos", "calibres", "compradores", "origenes", "bancos", "productores", "silos-calibre", "silos-pulmon", "bodegas" };
            var esGlobal = !catalogosConSede.Contains(catalogo);

            string tabla = GetTabla(catalogo);
            if (tabla == null) return BadRequest("Catálogo no válido");

            var idParam = GetIdParam(catalogo, id);

            if (!esGlobal && sedeId != 0) {
                var sedeRegistro = await _db.QueryFirstOrDefaultAsync<int?>(
                    $"SELECT sede_id FROM {tabla} WHERE id = @id", idParam);

                if (sedeRegistro == null) return NotFound();
                if (sedeRegistro != sedeId && sedeRegistro != 0 && sedeRegistro.HasValue)
                    return StatusCode(403, "No tiene permiso para modificar este registro");
            }

            await _db.ExecuteAsync($"UPDATE {tabla} SET activo = @Activo, updated_at = GETDATE() WHERE id = @id",
                new { item.Activo, id = ((dynamic)idParam).id });
            return Ok();
        }

        [HttpDelete("{catalogo}/{id}")]
        public async Task<IActionResult> DeleteItem(string catalogo, string id, [FromQuery] int sedeId) {
            var catalogosConSede = new[] { "granos", "calibres", "compradores", "origenes", "bancos", "productores", "silos-calibre", "silos-pulmon", "bodegas" };
            var esGlobal = !catalogosConSede.Contains(catalogo);

            string tabla = GetTabla(catalogo);
            if (tabla == null) return BadRequest("Catálogo no válido");

            var idParam = GetIdParam(catalogo, id);

            if (!esGlobal && sedeId != 0) {
                var sedeRegistro = await _db.QueryFirstOrDefaultAsync<int?>(
                    $"SELECT sede_id FROM {tabla} WHERE id = @id", idParam);

                if (sedeRegistro == null) return NotFound();
                if (sedeRegistro != sedeId && sedeRegistro != 0 && sedeRegistro.HasValue)
                    return StatusCode(403, "No tiene permiso para eliminar este registro");
            }

            await _db.ExecuteAsync($"DELETE FROM {tabla} WHERE id = @id", idParam);
            return Ok();
        }
    }
}