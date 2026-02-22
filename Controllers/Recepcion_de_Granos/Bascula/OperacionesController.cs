using Microsoft.AspNetCore.Mvc;        // Para ControllerBase, ApiController, HttpGet, etc.
using System.Data;                     // Para IDbConnection
using Dapper;                          // Para los métodos de extensión .Query y .Execute
using SistemaAlazan.Models;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("[controller]")]
    public class OperacionesController : ControllerBase
    {
        private readonly IDbConnection _db;

        public OperacionesController(IDbConnection db)
        {
            _db = db;
        }

        [HttpGet("bascula/pendientes")]
        public async Task<IActionResult> GetBasculaPendientes()
        {
            // Nota: Asegúrate de que los nombres de las columnas en SQL coincidan 
            // con las propiedades de tu clase BasculaRecepcion
            var sql = "SELECT * FROM dbo.bascula_recepciones WHERE status = 'PENDIENTE_ANALISIS'";
            var resultados = await _db.QueryAsync<BasculaRecepcion>(sql);
            return Ok(resultados);
        }

        [HttpPost("bascula/registro")]
        public async Task<IActionResult> PostBascula(BasculaRecepcion b, [FromQuery] int sedeId)
        {
            // IMPORTANTE: Usar sedeId del query parameter (sede seleccionada)
            b.sede_id = sedeId;

            const string sql = @"
                INSERT INTO dbo.bascula_recepciones (ticket_numero, productor_id, peso_bruto_kg, tara_kg, sede_id)
                VALUES (@TicketNumero, @ProductorId, @PesoBrutoKg, @TaraKg, @sede_id);
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            var id = await _db.QuerySingleAsync<long>(sql, b);
            return Ok(new { id, message = "Ticket registrado correctamente" });
        }
    }
}