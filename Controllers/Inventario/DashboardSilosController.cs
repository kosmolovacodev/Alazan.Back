using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DashboardSilosController : ControllerBase
    {
        private readonly IDbConnection _db;
        public DashboardSilosController(IDbConnection db) => _db = db;

        // GET /api/dashboardsilos/resumen?sedeId=8&fechaInicio=2026-04-01&fechaFin=2026-04-30
        // Devuelve totales por silo agrupados por fecha y calibre
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] int sedeId,
            [FromQuery] string? fechaInicio = null,
            [FromQuery] string? fechaFin = null)
        {
            try
            {
                DateTime? dInicio = fechaInicio != null ? DateTime.Parse(fechaInicio) : (DateTime?)null;
                DateTime? dFin    = fechaFin   != null ? DateTime.Parse(fechaFin)   : (DateTime?)null;

                // Si no se indica rango, traer los últimos 30 días
                if (dInicio == null) dInicio = DateTime.Today.AddDays(-29);
                if (dFin    == null) dFin    = DateTime.Today;

                var rows = await _db.QueryAsync<dynamic>(@"
                    SELECT
                        bodega_nombre                              AS bodegaNombre,
                        CAST(fecha_ingreso AS DATE)                AS fecha,
                        calibre,
                        COUNT(*)                                   AS recepciones,
                        SUM(kg_bruto)                             AS totalKgBruto,
                        SUM(kg_neto)                              AS totalKgNeto,
                        SUM(CAST(toneladas_brutas AS DECIMAL(18,4))) AS totalTonBrutas,
                        SUM(CAST(toneladas_netas  AS DECIMAL(18,4))) AS totalTonNetas
                    FROM dbo.inventario_silos
                    WHERE sede_id = @SedeId
                      AND CAST(fecha_ingreso AS DATE) >= @FechaInicio
                      AND CAST(fecha_ingreso AS DATE) <= @FechaFin
                    GROUP BY
                        bodega_nombre,
                        CAST(fecha_ingreso AS DATE),
                        calibre
                    ORDER BY
                        CAST(fecha_ingreso AS DATE) DESC,
                        bodega_nombre,
                        calibre",
                    new { SedeId = sedeId, FechaInicio = dInicio, FechaFin = dFin });

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET /api/dashboardsilos/detalle?sedeId=8&bodegaNombre=Silo+2&fecha=2026-04-21
        // Devuelve los tickets individuales de un silo en una fecha
        [HttpGet("detalle")]
        public async Task<IActionResult> GetDetalle(
            [FromQuery] int sedeId,
            [FromQuery] string bodegaNombre,
            [FromQuery] string fecha)
        {
            try
            {
                var rows = await _db.QueryAsync<dynamic>(@"
                    SELECT
                        inv.id,
                        inv.ticket_numero                        AS ticketNumero,
                        inv.calibre,
                        inv.kg_bruto                            AS kgBruto,
                        inv.kg_neto                             AS kgNeto,
                        CAST(inv.toneladas_brutas AS DECIMAL(18,4)) AS toneladasBrutas,
                        CAST(inv.toneladas_netas  AS DECIMAL(18,4)) AS toneladasNetas,
                        inv.status,
                        FORMAT(inv.fecha_ingreso, 'HH:mm')      AS hora,
                        ISNULL(b.productor, '—')                AS productor
                    FROM dbo.inventario_silos inv
                    LEFT JOIN dbo.boletas b ON b.bascula_id = inv.bascula_id
                    WHERE inv.sede_id       = @SedeId
                      AND inv.bodega_nombre = @BodegaNombre
                      AND CAST(inv.fecha_ingreso AS DATE) = @Fecha
                    ORDER BY inv.fecha_ingreso",
                    new { SedeId = sedeId, BodegaNombre = bodegaNombre, Fecha = fecha });

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET /api/dashboardsilos/totales?sedeId=8
        // Totales acumulados por silo (todos los registros sin importar fecha)
        [HttpGet("totales")]
        public async Task<IActionResult> GetTotales([FromQuery] int sedeId)
        {
            try
            {
                var rows = await _db.QueryAsync<dynamic>(@"
                    SELECT
                        bodega_nombre                              AS bodegaNombre,
                        calibre,
                        COUNT(*)                                   AS recepciones,
                        SUM(kg_neto)                              AS totalKgNeto,
                        SUM(CAST(toneladas_netas AS DECIMAL(18,4))) AS totalTonNetas,
                        MIN(CAST(fecha_ingreso AS DATE))           AS primerFecha,
                        MAX(CAST(fecha_ingreso AS DATE))           AS ultimaFecha
                    FROM dbo.inventario_silos
                    WHERE sede_id = @SedeId
                    GROUP BY bodega_nombre, calibre
                    ORDER BY bodega_nombre, calibre",
                    new { SedeId = sedeId });

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
