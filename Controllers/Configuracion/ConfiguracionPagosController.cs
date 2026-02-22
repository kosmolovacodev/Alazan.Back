using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data;
using SistemaAlazan.Models;

namespace SistemaAlazan.Controllers
{
    // [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class ConfiguracionPagosController : ControllerBase
    {
        private readonly IDbConnection _db;

        public ConfiguracionPagosController(IDbConnection db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetConfiguracion([FromQuery] int sedeId)
        {
            try
            {
                var dto = new PagosConfigDto();

                // 1. General (Horarios, Tiempos y Validaciones)
                string sqlGeneral = @"
                    SELECT
                        id,
                        horario_limite_solicitud AS HorarioLimiteSolicitud,
                        alerta_dias_festivos AS AlertaDiasFestivos,
                        dias_autorizacion AS DiasAutorizacion,
                        dias_ejecucion AS DiasEjecucion,
                        dias_alerta_vencimiento AS DiasAlertaVencimiento,
                        validar_topes_diarios AS ValidarTopesDiarios,
                        validar_dias_habiles AS ValidarDiasHabiles,
                        validar_horario_limite AS ValidarHorarioLimite,
                        requiere_folio_pago AS RequiereFolioPago,
                        requiere_comprobante_pago AS RequiereComprobantePago,
                        permitir_pago_parcial AS PermitirPagoParcial,
                        monto_minimo_pago AS MontoMinimoPago
                    FROM dbo.Configuracion_Pagos_General WHERE sede_id = @sedeId";

                dto.General = await _db.QueryFirstOrDefaultAsync<PagosGeneralDto>(sqlGeneral, new { sedeId }) ?? new PagosGeneralDto();

                // 2. Status
                string sqlStatus = @"
                    SELECT id, nombre, color, descripcion, orden, activo,
                           bloquea_edicion AS BloqueaEdicion,
                           requiere_aprobacion AS RequiereAprobacion
                    FROM dbo.Configuracion_Pagos_Status
                    WHERE sede_id = @sedeId ORDER BY orden";
                dto.Status = (await _db.QueryAsync<PagosStatusDto>(sqlStatus, new { sedeId })).ToList();

                // 3. Formas de Pago
                string sqlFormas = @"
                    SELECT id, nombre, codigo, activo,
                           requiere_clabe AS RequiereCLABE,
                           requiere_cuenta AS RequiereCuenta
                    FROM dbo.Configuracion_Pagos_Formas
                    WHERE sede_id = @sedeId ORDER BY nombre";
                dto.FormasPago = (await _db.QueryAsync<PagosFormaDto>(sqlFormas, new { sedeId })).ToList();

                // 4. Días Hábiles
                string sqlDias = "SELECT id, dia, activo FROM dbo.Configuracion_Pagos_Dias WHERE sede_id = @sedeId";
                dto.DiasHabiles = (await _db.QueryAsync<PagosDiaDto>(sqlDias, new { sedeId })).ToList();

                // 5. Sedes (Topes) - Solo devuelve la sede actual para usuarios normales, todas para admin
                string sqlSedes = @"
                    SELECT id, nombre_sede AS NombreSede, ciudad, estado,
                           tope_diario AS TopeDiario, activo
                    FROM dbo.sedes_catalogo
                    WHERE @sedeId = 0 OR id = @sedeId
                    ORDER BY nombre_sede";
                dto.TopesSede = (await _db.QueryAsync<PagosSedeDto>(sqlSedes, new { sedeId })).ToList();

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar configuración de pagos", error = ex.Message });
            }
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarConfiguracion([FromBody] PagosConfigDto dto, [FromQuery] int sedeId)
        {
            if (dto == null) return BadRequest("Datos inválidos");
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var transaction = _db.BeginTransaction();

            try
            {
                // 1. Actualizar/Insertar General
                var existeGeneral = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.Configuracion_Pagos_General WHERE sede_id = @sedeId",
                    new { sedeId }, transaction);

                if (existeGeneral > 0)
                {
                    var sqlGeneral = @"
                        UPDATE dbo.Configuracion_Pagos_General SET
                            horario_limite_solicitud = @HorarioLimiteSolicitud,
                            alerta_dias_festivos = @AlertaDiasFestivos,
                            dias_autorizacion = @DiasAutorizacion,
                            dias_ejecucion = @DiasEjecucion,
                            dias_alerta_vencimiento = @DiasAlertaVencimiento,
                            validar_topes_diarios = @ValidarTopesDiarios,
                            validar_dias_habiles = @ValidarDiasHabiles,
                            validar_horario_limite = @ValidarHorarioLimite,
                            requiere_folio_pago = @RequiereFolioPago,
                            requiere_comprobante_pago = @RequiereComprobantePago,
                            permitir_pago_parcial = @PermitirPagoParcial,
                            monto_minimo_pago = @MontoMinimoPago
                        WHERE sede_id = @SedeId";
                    await _db.ExecuteAsync(sqlGeneral, new { dto.General.HorarioLimiteSolicitud, dto.General.AlertaDiasFestivos,
                        dto.General.DiasAutorizacion, dto.General.DiasEjecucion, dto.General.DiasAlertaVencimiento,
                        dto.General.ValidarTopesDiarios, dto.General.ValidarDiasHabiles, dto.General.ValidarHorarioLimite,
                        dto.General.RequiereFolioPago, dto.General.RequiereComprobantePago, dto.General.PermitirPagoParcial,
                        dto.General.MontoMinimoPago, SedeId = sedeId }, transaction);
                }

                // 2. Status (solo actualizar si pertenecen a la sede)
                if (dto.Status != null && dto.Status.Any())
                {
                    foreach (var status in dto.Status)
                    {
                        await _db.ExecuteAsync(@"UPDATE dbo.Configuracion_Pagos_Status SET
                                activo = @Activo, orden = @Orden
                                WHERE id = @Id AND sede_id = @SedeId",
                            new { status.Activo, status.Orden, status.Id, SedeId = sedeId }, transaction);
                    }
                }

                // 3. Formas Pago (solo actualizar si pertenecen a la sede)
                if (dto.FormasPago != null && dto.FormasPago.Any())
                {
                    foreach (var forma in dto.FormasPago)
                    {
                        await _db.ExecuteAsync(@"UPDATE dbo.Configuracion_Pagos_Formas SET
                                activo = @Activo, requiere_clabe = @RequiereCLABE, requiere_cuenta = @RequiereCuenta
                                WHERE id = @Id AND sede_id = @SedeId",
                            new { forma.Activo, forma.RequiereCLABE, forma.RequiereCuenta, forma.Id, SedeId = sedeId }, transaction);
                    }
                }

                // 4. Días Hábiles (solo actualizar si pertenecen a la sede)
                if (dto.DiasHabiles != null && dto.DiasHabiles.Any())
                {
                    foreach (var dia in dto.DiasHabiles)
                    {
                        await _db.ExecuteAsync(
                            "UPDATE dbo.Configuracion_Pagos_Dias SET activo = @Activo WHERE id = @Id AND sede_id = @SedeId",
                            new { dia.Activo, dia.Id, SedeId = sedeId }, transaction);
                    }
                }

                // 5. Sedes (Topes) - Solo admin puede actualizar o solo la sede actual
                if (dto.TopesSede != null && dto.TopesSede.Any())
                {
                    foreach (var sede in dto.TopesSede)
                    {
                        // Solo admin (sedeId=0) puede actualizar cualquier sede
                        // Usuarios normales solo pueden actualizar su propia sede
                        if (sedeId == 0 || sede.Id == sedeId)
                        {
                            await _db.ExecuteAsync(@"UPDATE dbo.sedes_catalogo SET
                                    activo = @Activo, tope_diario = @TopeDiario
                                    WHERE id = @Id",
                                new { sede.Activo, sede.TopeDiario, sede.Id }, transaction);
                        }
                    }
                }

                transaction.Commit();
                return Ok(new { message = "Configuración de pagos guardada" });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "Error al guardar", error = ex.Message });
            }
        }
    }
}