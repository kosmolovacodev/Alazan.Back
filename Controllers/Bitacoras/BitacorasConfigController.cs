using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("bitacoras-config")]
    public class BitacorasConfigController : ControllerBase
    {
        private readonly IDbConnection _db;
        public BitacorasConfigController(IDbConnection db) => _db = db;

        // ─── GET /bitacoras-config ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    id,
                    codigo_bitacora   AS codigoBitacora,
                    periodicidad,
                    dia_semana        AS diaSemana,
                    rol_operativo     AS rolOperativo,
                    firmas_operativo  AS firmasOperativo,
                    rol_supervisor    AS rolSupervisor,
                    firmas_supervisor AS firmasSupervisor,
                    rol_gerente       AS rolGerente,
                    firmas_gerente    AS firmasGerente,
                    firma_recepcion   AS firmaRecepcion,
                    updated_at        AS updatedAt
                  FROM dbo.bitacoras_config");
            return Ok(rows);
        }

        // ─── PUT /bitacoras-config/{codigo} ── Upsert config por bitácora ──
        [HttpPut("{codigo}")]
        public async Task<IActionResult> Upsert(string codigo, [FromBody] BitacoraConfigRequest req)
        {
            await _db.ExecuteAsync(
                @"IF EXISTS (SELECT 1 FROM dbo.bitacoras_config WHERE codigo_bitacora = @Codigo)
                      UPDATE dbo.bitacoras_config SET
                          periodicidad      = @Periodicidad,
                          dia_semana        = @DiaSemana,
                          rol_operativo     = @RolOperativo,
                          firmas_operativo  = @FirmasOperativo,
                          rol_supervisor    = @RolSupervisor,
                          firmas_supervisor = @FirmasSupervisor,
                          rol_gerente       = @RolGerente,
                          firmas_gerente    = @FirmasGerente,
                          firma_recepcion   = @FirmaRecepcion,
                          updated_at        = GETDATE()
                      WHERE codigo_bitacora = @Codigo
                  ELSE
                      INSERT INTO dbo.bitacoras_config
                          (codigo_bitacora, periodicidad, dia_semana,
                           rol_operativo, firmas_operativo,
                           rol_supervisor, firmas_supervisor,
                           rol_gerente, firmas_gerente,
                           firma_recepcion, updated_at)
                      VALUES
                          (@Codigo, @Periodicidad, @DiaSemana,
                           @RolOperativo, @FirmasOperativo,
                           @RolSupervisor, @FirmasSupervisor,
                           @RolGerente, @FirmasGerente,
                           @FirmaRecepcion, GETDATE())",
                new
                {
                    Codigo            = codigo,
                    req.Periodicidad,
                    req.DiaSemana,
                    req.RolOperativo,
                    req.FirmasOperativo,
                    req.RolSupervisor,
                    req.FirmasSupervisor,
                    req.RolGerente,
                    req.FirmasGerente,
                    req.FirmaRecepcion,
                });

            return Ok(new { ok = true });
        }

        // ─── GET /bitacoras-config/nip-global ───────────────────────────
        [HttpGet("nip-global")]
        public async Task<IActionResult> GetNip()
        {
            var row = await _db.QueryFirstOrDefaultAsync(
                @"SELECT TOP 1
                    longitud_nip   AS longitudNip,
                    intentos,
                    tiempo_bloqueo AS tiempoBloqueo
                  FROM dbo.bitacoras_nip_config");
            return Ok(row ?? new { longitudNip = 4, intentos = 3, tiempoBloqueo = 15 });
        }

        // ─── PUT /bitacoras-config/nip-global ───────────────────────────
        [HttpPut("nip-global")]
        public async Task<IActionResult> PutNip([FromBody] NipConfigRequest req)
        {
            await _db.ExecuteAsync(
                @"IF EXISTS (SELECT 1 FROM dbo.bitacoras_nip_config)
                      UPDATE TOP(1) dbo.bitacoras_nip_config SET
                          longitud_nip   = @LongitudNip,
                          intentos       = @Intentos,
                          tiempo_bloqueo = @TiempoBloqueo,
                          updated_at     = GETDATE()
                  ELSE
                      INSERT INTO dbo.bitacoras_nip_config (longitud_nip, intentos, tiempo_bloqueo, updated_at)
                      VALUES (@LongitudNip, @Intentos, @TiempoBloqueo, GETDATE())",
                new { req.LongitudNip, req.Intentos, req.TiempoBloqueo });
            return Ok(new { ok = true });
        }
    }

    public class BitacoraConfigRequest
    {
        public string Periodicidad      { get; set; } = "Diaria";
        public string? DiaSemana        { get; set; }
        public bool   RolOperativo      { get; set; }
        public int    FirmasOperativo   { get; set; } = 2;
        public bool   RolSupervisor     { get; set; }
        public int    FirmasSupervisor  { get; set; } = 1;
        public bool   RolGerente        { get; set; }
        public int    FirmasGerente     { get; set; } = 1;
        public bool   FirmaRecepcion    { get; set; }
    }

    public class NipConfigRequest
    {
        public int LongitudNip   { get; set; } = 4;
        public int Intentos      { get; set; } = 3;
        public int TiempoBloqueo { get; set; } = 15;
    }
}
