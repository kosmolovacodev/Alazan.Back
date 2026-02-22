using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using SistemaAlazan.Models;

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

        // --- PRODUCTORES (Filtrado por sede) ---
        [HttpGet("productores")]
        public async Task<IActionResult> GetProductores([FromQuery] int sedeId) {
            // Admin global (sedeId=0) ve todos
            // Usuarios normales ven: sus registros + compartidos (sede_id=0 o NULL)
            string sql = @"SELECT id, nombre, telefono, activo, atiende
                          FROM dbo.productores
                          WHERE @sedeId = 0 OR sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL";
            return Ok(await _db.QueryAsync(sql, new { sedeId }));
        }

        [HttpPost("productores")]
        public async Task<IActionResult> AddProductor([FromBody] ProductorNuevoReq item, [FromQuery] int sedeId) 
        {
            try 
            {
                // Asegúrate de que ProductorNuevoReq tenga Sede_id
                var sql = @"INSERT INTO productores (nombre, telefono, tipo_persona, atiende, activo, created_at, updated_at, sede_id) 
                            VALUES (@nombre, @telefono, @tipo_persona, @atiende, 1, GETDATE(), GETDATE(), @sede_id);
                            SELECT CAST(SCOPE_IDENTITY() as int);";
                var parameters = new {
                    item.nombre,
                    item.telefono,
                    item.tipo_persona,
                    item.atiende,
                    // item.rfc,
                    sede_id = sedeId // Maps the variable 'sedeId' to the SQL param '@sede_id'
                };

                var id = await _db.QuerySingleAsync<int>(sql, parameters);
                return Ok(new { id, nombre = item.nombre });
            }
            catch (Exception ex) {
                return StatusCode(500, $"Error: {ex.Message}");
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
                        item.Calibre_id, item.Capacidad_toneladas, item.Descripcion, item.Tipo, item.Grano_id
                    };
                }
                else
                {
                    int.TryParse(id, out var intId);
                    parameters = new {
                        Id = intId,
                        item.Nombre, item.Requiere_analisis, item.Telefono, item.Municipio,
                        item.Estado, item.Region, item.Codigo, item.Ciudad, item.Tope_diario,
                        item.Calibre_id, item.Capacidad_toneladas, item.Descripcion, item.Tipo, item.Grano_id
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