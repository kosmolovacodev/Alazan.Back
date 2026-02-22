using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using SistemaAlazan.Models;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BasculaController : ControllerBase
    {
        private readonly IDbConnection _db;

        public BasculaController(IDbConnection db)
        {
            _db = db;
        }

        [HttpGet("registros")]
        public async Task<IActionResult> GetRegistros([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"SELECT
                            b.id,
                            b.ticket_numero AS ticket,
                            COALESCE(bol.folio, b.boleta_numero) AS boleta,
                            b.fecha_hora AS fecha,
                            b.peso_bruto_kg AS pesoBruto,
                            b.tara_kg AS tara,
                            b.peso_neto_kg AS pesoNeto,
                            b.status,

                            p.tipo_persona,

                            CASE
                                WHEN p.tipo_persona = 'Moral' THEN p.atiende
                                WHEN p.tipo_persona = 'Fisica' THEN p.nombre
                                ELSE NULL
                            END AS productor,

                            COALESCE(bol.comprador, c.nombre) AS comprador,
                            g.nombre AS grano_nombre,
                            o.municipio AS origen_nombre

                        FROM dbo.bascula_recepciones b
                        LEFT JOIN dbo.productores p ON b.productor_id = p.id
                        LEFT JOIN dbo.compradores_catalogo c ON TRY_CAST(b.comprador_id AS VARCHAR(50)) = TRY_CAST(c.id AS VARCHAR(50))
                        LEFT JOIN dbo.granos_catalogo g ON b.grano_id = g.id
                        LEFT JOIN dbo.origenes_catalogo o ON b.origen_id = o.id
                        LEFT JOIN dbo.boletas bol ON bol.bascula_id = b.id
                         WHERE (@sedeId = 0 OR b.sede_id = @sedeId)
                         ORDER BY b.id asc ";

                var registros = await _db.QueryAsync<object>(sql, new { sedeId });
                return Ok(registros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener registros: {ex.Message}");
            }
        }

        // POST: api/bascula/guardar
        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] BasculaRecepcion modelo, [FromQuery] int sedeId)
        {
            try
            {
                modelo.sede_id = sedeId;

                // 1. Manejo del Catálogo de Choferes (Igual que antes)
                if (!string.IsNullOrWhiteSpace(modelo.chofer) && !string.IsNullOrWhiteSpace(modelo.placas))
                {
                    var sqlInsertCatalogo = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.catalogo_choferes 
                                    WHERE nombre = @chofer AND placas = @placas AND sede_id = @sedeId)
                        BEGIN
                            INSERT INTO dbo.catalogo_choferes (nombre, placas, sede_id, activo)
                            VALUES (@chofer, @placas, @sedeId, 1)
                        END";

                    await _db.ExecuteAsync(sqlInsertCatalogo, new { 
                        chofer = modelo.chofer.Trim(), 
                        placas = modelo.placas.Trim(), 
                        sedeId 
                    });
                }

                // 2. Lógica de Guardado con WHERE ID (Upsert)
                string sql;
                if (modelo.id > 0) 
                {
                    // Si el ID existe, actualizamos usando el ID como filtro
                    sql = @"UPDATE dbo.bascula_recepciones SET 
                                ticket_numero = @ticket_numero,
                                productor_id = @productor_id,
                                chofer = @chofer,
                                placas = @placas,
                                peso_bruto_kg = @peso_bruto_kg,
                                grano_id = @grano_id,
                                origen_id = @origen_id,
                                comprador_id = @comprador_id,
                                status = @status,
                                datos_adicionales = @datos_adicionales,
                                updated_at = SYSDATETIMEOFFSET()
                            WHERE id = @id;
                            SELECT @id;";
                }
                else 
                {
                    // Si el ID es 0, es un registro nuevo (INSERT)
                    sql = @"INSERT INTO dbo.bascula_recepciones
                                (ticket_numero, fecha_hora, productor_id, chofer, placas,
                                peso_bruto_kg, grano_id, origen_id, comprador_id,
                                status, datos_adicionales, boleta_numero, usuario_registro_id, sede_id,
                                created_at, updated_at)
                            VALUES
                                (@ticket_numero, SYSDATETIMEOFFSET(), @productor_id, @chofer, @placas,
                                @peso_bruto_kg, @grano_id, @origen_id, @comprador_id,
                                @status, @datos_adicionales, @boleta_numero, @usuario_registro_id, @sede_id,
                                SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
                            SELECT CAST(SCOPE_IDENTITY() as int);";
                }

                var resultId = await _db.QuerySingleAsync<int>(sql, modelo);
                return Ok(resultId);
            }
            catch (Exception ex)
            {
                // Si el error es por ticket duplicado, damos un mensaje más amigable
                if (ex.Message.Contains("UNIQUE KEY"))
                    return StatusCode(409, "El número de ticket ya existe. Por favor, use otro o actualice el registro actual.");
                    
                return StatusCode(500, $"Error en SQL: {ex.Message}");
            }
        }

        // GET: bascula/choferes?sedeId=1
            [HttpGet("choferes")]
            public async Task<IActionResult> GetChoferes([FromQuery] int sedeId)
            {
                try
                {
                    // Ahora consultamos la tabla de catálogo dedicada
                    var sql = @"SELECT id, nombre AS chofer, placas 
                                FROM dbo.catalogo_choferes 
                                WHERE (@sedeId = 0 OR sede_id = @sedeId)
                                ORDER BY nombre ASC";

                    var choferes = await _db.QueryAsync<dynamic>(sql, new { sedeId });
                    return Ok(choferes);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error: {ex.Message}");
                }
            }
        // GET: bascula/ultimo-grano?sedeId=1
        [HttpGet("ultimo-grano")]
        public async Task<IActionResult> GetUltimoGrano([FromQuery] int sedeId)
        {
            try
            {
                var sql = @"
                    SELECT TOP 1 br.grano_id AS granoId
                    FROM dbo.bascula_recepciones br
                    WHERE (@sedeId = 0 OR br.sede_id = @sedeId)
                    ORDER BY br.fecha_hora DESC";
                var result = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { sedeId });
                return Ok(result ?? new { granoId = (int?)null });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener último grano: {ex.Message}");
            }
        }

        [HttpGet("ultimo-ticket")]
        public async Task<IActionResult> GetUltimoTicket([FromQuery] int sedeId)
        {
            // COALESCE devuelve 0 si la tabla está vacía
            // Filtra por sede para que cada sede tenga su propia secuencia de tickets
            var sql = @"SELECT COALESCE(MAX(TRY_CAST(ticket_numero AS INT)), 0)
                            FROM dbo.bascula_recepciones
                       WHERE sede_id = @sedeId";
            var ultimo = await _db.ExecuteScalarAsync<int>(sql, new { sedeId });
            return Ok(ultimo);
        }

    }
}