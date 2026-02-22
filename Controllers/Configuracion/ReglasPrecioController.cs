using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Alazan.API.Models; // Asegúrate de que apunte a tu carpeta de modelos

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/reglas-precio")]
  [Route("reglas-precio")]

    public class ReglasPrecioController : ControllerBase
    {
        private readonly IDbConnection _db;
        public ReglasPrecioController(IDbConnection db) => _db = db;

        // --- 1. PARÁMETROS BASE (CON LOGS) ---
        
        [HttpGet("config-actual")]
        public async Task<IActionResult> GetConfig([FromQuery] int sedeId) 
        {
            // Se filtra estrictamente por sedeId
            var sql = @"SELECT TOP 1 
                        precio_base_usd AS precioBaseUsd, 
                        tipo_cambio_fix AS tipoCambio, 
                        precio_base_mxn AS precioBaseMxn, 
                        url_api_cambio AS urlApi 
                        FROM dbo.ParametrosBase 
                        WHERE sede_id = @sedeId 
                        ORDER BY fecha_registro DESC";
                        
            var config = await _db.QueryFirstOrDefaultAsync<ConfigPrecioDto>(sql, new { sedeId });
            return Ok(config);
        }

        [HttpPost("guardar-base")]
        public async Task<IActionResult> GuardarBase([FromBody] ConfigPrecioDto dto, [FromQuery] int sedeId)
        {
            // IMPORTANTE: Usar sedeId del query parameter (sede seleccionada)
            dto.SedeId = sedeId;

            var sql = @"INSERT INTO dbo.ParametrosBase
                        (precio_base_usd, tipo_cambio_fix, url_api_cambio, usuario_registro, fecha_registro, sede_id)
                        VALUES (@PrecioBaseUsd, @TipoCambio, @UrlApi, @UsuarioRegistro, SYSDATETIMEOFFSET(), @SedeId)";

            await _db.ExecuteAsync(sql, new{
                dto.PrecioBaseUsd,
                dto.TipoCambio,
                dto.UrlApi,
                dto.UsuarioRegistro,
                dto.SedeId
            });
            return Ok(new { message = "Configuración por sede guardada" });
        }

        // Cambia la definición del ruteo a esto:
        [HttpGet("obtener-usuario/{id}")] 
        public async Task<IActionResult> ObtenerUsuario([FromRoute] string id) // Agrega [FromRoute]
        {
            // Asegúrate de que no haya espacios invisibles
            var userId = id.Trim(); 

            var sql = "SELECT nombre_completo FROM dbo.usuarios WHERE auth_user_id = @id";
            
            // Si tu columna en SQL es UNIQUEIDENTIFIER, Dapper suele convertirlo solo, 
            // pero si es VARCHAR, asegúrate de que el objeto coincida.
            var nombre = await _db.QueryFirstOrDefaultAsync<string>(sql, new { id = userId });

            if (string.IsNullOrEmpty(nombre))
            {
                return NotFound(new { message = $"No se encontró el nombre para el ID {userId}" });
            }

            return Ok(new { nombre });
        }

        // --- 2. CATÁLOGOS (CALIBRES Y DESCUENTOS PRECIO) ---

        [HttpGet("catalogos")]
            public async Task<IActionResult> GetCatalogos([FromQuery] int sedeId, [FromQuery] int? granoId = null)
            {
                // Filtrar niveles de exportación por sede y grano
                var sqlExport = @"SELECT * FROM NivelesPrecioExportacion
                                WHERE sede_id = @sedeId
                                  AND (@granoId IS NULL OR grano_id = @granoId)
                                ORDER BY porcentaje_export_label DESC";
                var export = await _db.QueryAsync(sqlExport, new { sedeId, granoId });

                // Filtrar calibres por sede y grano
                var sqlCalibres = @"SELECT * FROM dbo.DescuentosCalibre_Catalogo
                                    WHERE sede_id = @sedeId
                                      AND (@granoId IS NULL OR grano_id = @granoId)
                                    ORDER BY calibre";
                var calibres = await _db.QueryAsync(sqlCalibres, new { sedeId, granoId });

                // Filtrar descuentos de precio por sede y grano
                var sqlPrecios = @"SELECT * FROM dbo.DescuentosPrecio_Catalogo
                                WHERE sede_id = @sedeId
                                  AND (@granoId IS NULL OR grano_id = @granoId)
                                ORDER BY descuento_mxn";
                var precios = await _db.QueryAsync(sqlPrecios, new { sedeId, granoId });

                return Ok(new { export, calibres, precios });
            }

        // --- 3. OPERACIONES DE CATÁLOGOS (POST/DELETE) ---

        // Agregar Calibre (DC) - Ahora con sedeId y granoId desde query parameter
        [HttpPost("calibres")]
        public async Task<IActionResult> AddCalibre([FromBody] DescuentoCalibre dc, [FromQuery] int sedeId, [FromQuery] int? granoId = null)
        {
            var sql = @"INSERT INTO dbo.DescuentosCalibre_Catalogo (codigo, calibre, descuento_kg_ton, sede_id, grano_id)
                        VALUES (@Codigo, @Calibre, @descuento_kg_ton, @SedeId, @GranoId)";

            await _db.ExecuteAsync(sql, new { dc.Codigo, dc.Calibre, dc.descuento_kg_ton, SedeId = sedeId, GranoId = granoId });

            return Ok(new { message = "Calibre registrado correctamente" });
        }

        [HttpDelete("calibres/{id}")]
        public async Task<IActionResult> DeleteCalibre(int id)
        {
            await _db.ExecuteAsync("DELETE FROM dbo.DescuentosCalibre_Catalogo WHERE id = @id", new { id });
            return Ok(new { message = "Calibre eliminado" });
        }

        [HttpPut("calibres/{id}")]
        public async Task<IActionResult> UpdateCalibre(int id, [FromBody] DescuentoCalibre dc)
        {
            var sql = @"UPDATE dbo.DescuentosCalibre_Catalogo
                        SET descuento_kg_ton = @descuento_kg_ton
                        WHERE id = @id AND (@grano_id IS NULL OR grano_id = @grano_id)";
            await _db.ExecuteAsync(sql, new { dc.descuento_kg_ton, dc.grano_id, id });
            return Ok(new { message = "Calibre actualizado correctamente" });
        }

        [HttpPost("descuentos-precio")]
        public async Task<IActionResult> AddDescuentoPrecio([FromBody] DescuentoPrecio dp, [FromQuery] int sedeId, [FromQuery] int? granoId = null)
        {
            // Incluimos sede_id y grano_id desde el query parameter
            var sql = "INSERT INTO dbo.DescuentosPrecio_Catalogo (codigo, descuento_mxn, sede_id, grano_id) VALUES (@Codigo, @descuento_mxn, @SedeId, @GranoId)";

            await _db.ExecuteAsync(sql, new { dp.Codigo, dp.descuento_mxn, SedeId = sedeId, GranoId = granoId });

            return Ok(new { message = "Registro exitoso con código manual" });
        }

        [HttpPut("descuentos-precio/{id}")]
        public async Task<IActionResult> UpdateDescuentoPrecio(int id, [FromBody] DescuentoPrecio dp)
        {
            var sql = @"UPDATE dbo.DescuentosPrecio_Catalogo
                        SET descuento_mxn = @descuento_mxn
                        WHERE id = @id AND (@grano_id IS NULL OR grano_id = @grano_id)";

            await _db.ExecuteAsync(sql, new { dp.descuento_mxn, dp.grano_id, id });

            return Ok(new { message = "Descuento de precio actualizado correctamente" });
        }

        [HttpDelete("descuentos-precio/{id}")]
        public async Task<IActionResult> DeleteDescuentoPrecio(int id)
        {
            // Nota: Podría fallar si algún P1-P27 lo está usando (FK constraint)
            await _db.ExecuteAsync("DELETE FROM dbo.DescuentosPrecio_Catalogo WHERE id = @id", new { id });
            return Ok(new { message = "Descuento eliminado" });
        }

        
        [HttpGet("ultimo-codigo-precio")]
        public async Task<IActionResult> GetUltimoCodigoPrecio()
        {
            // Buscamos el código con el número más alto directamente en la DB
            var sql = @"SELECT TOP 1 codigo FROM dbo.DescuentosPrecio_Catalogo 
                        ORDER BY CAST(REPLACE(codigo, 'DP', '') AS INT) DESC";
            
            var ultimoCodigo = await _db.QueryFirstOrDefaultAsync<string>(sql);
            return Ok(new { ultimo = ultimoCodigo });
        }

        // --- 4. ACTUALIZAR NIVEL DE EXPORTACIÓN ---

        [HttpPut("niveles-exportacion/{id}")]
        public async Task<IActionResult> UpdateNivelExport(int id, [FromBody] NivelExportacion nivel)
        {
            var sql = @"UPDATE dbo.NivelesPrecioExportacion
                        SET descuento_precio_id = @DescuentoPrecioId,
                            vigente = @Vigente,
                            precio_final_mxn = @PrecioFinalMxn
                        WHERE id = @id AND (@grano_id IS NULL OR grano_id = @grano_id)";

            await _db.ExecuteAsync(sql, new {
                nivel.DescuentoPrecioId,
                nivel.Vigente,
                nivel.PrecioFinalMxn,
                nivel.grano_id,
                id
            });

            return Ok(new { message = "Nivel y precio actualizados" });
        }
       
       public class ActualizarPreciosRequest
        {
            public decimal PrecioBaseMxn { get; set; }
            public int SedeId { get; set; } // Agregamos SedeId para no afectar a otros
        }
        [HttpPost("actualizar-precios-masivo")]
        public async Task<IActionResult> ActualizarPreciosMasivo([FromBody] ActualizarPreciosRequest request)
        {
            // Agregamos WHERE n.sede_id = @SedeId para que el Admin no rompa los precios de otras sedes
            var sql = @"
                UPDATE n
                SET n.precio_final_mxn = @PrecioBaseMxn - ISNULL(dp.descuento_mxn, 0)
                FROM dbo.NivelesPrecioExportacion n
                LEFT JOIN dbo.DescuentosPrecio_Catalogo dp ON n.descuento_precio_id = dp.id
                WHERE n.vigente = 1 AND n.sede_id = @SedeId";

            await _db.ExecuteAsync(sql, new { request.PrecioBaseMxn, request.SedeId });
            
            return Ok(new { message = "Precios de la sede actualizados correctamente" });
        }
    }
}