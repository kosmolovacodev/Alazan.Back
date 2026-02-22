using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data;
using SistemaAlazan.Models; // Asegúrate de que este sea el namespace de tus modelos

namespace SistemaAlazan.Controllers
{
    // [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class ConfiguracionFacturacionController : ControllerBase
    {
        private readonly IDbConnection _db;

        public ConfiguracionFacturacionController(IDbConnection db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetConfiguracion([FromQuery] int sedeId)
        {
            try
            {
                var dto = new FacturacionConfigDto();

                // 1. General
                string sqlGeneral = @"
                    SELECT
                        id,
                        validar_rfc_sat AS ValidarRfcSat,
                        dias_alerta_vencimiento AS DiasAlertaVencimiento,
                        permitir_docs_vencidos AS PermitirDocsVencidos,
                        validar_formato_archivos AS ValidarFormatoArchivos,
                        tamano_maximo_mb AS TamanoMaximoMb,
                        retencion_federal_pct AS RetencionFederalPct,
                        retencion_isr_pct AS RetencionIsrPct,
                        retencion_iva_pct AS RetencionIvaPct,
                        aplicar_retencion_auto AS AplicarRetencionAuto,
                        longitud_rfc_fisica AS LongitudRfcFisica,
                        longitud_rfc_moral AS LongitudRfcMoral,
                        requiere_acta_moral AS RequiereActaMoral,
                        formatos_aceptados AS FormatosAceptados
                    FROM dbo.Configuracion_Facturacion_General WHERE sede_id = @sedeId";

                dto.General = await _db.QueryFirstOrDefaultAsync<FacturacionGeneralDto>(sqlGeneral, new { sedeId }) ?? new FacturacionGeneralDto();

                // 2. Documentos
                string sqlDocs = @"
                    SELECT id, nombre, formato, obligatorio,
                           requiere_vigencia AS RequiereVigencia,
                           dias_vigencia_default AS DiasVigenciaDefault,
                           aplica_persona_fisica AS AplicaPersonaFisica,
                           aplica_persona_moral AS AplicaPersonaMoral, activo
                    FROM dbo.Configuracion_Facturacion_Documentos
                    WHERE sede_id = @sedeId ORDER BY nombre";
                dto.Documentos = (await _db.QueryAsync<FacturacionDocumentoDto>(sqlDocs, new { sedeId })).ToList();

                // 3. Status
                string sqlStatus = @"
                    SELECT id, nombre, color_hex AS ColorHex, descripcion, orden,
                           bloquea_pago AS BloqueaPago, activo
                    FROM dbo.Configuracion_Facturacion_Status
                    WHERE sede_id = @sedeId ORDER BY orden";
                dto.StatusFlujo = (await _db.QueryAsync<FacturacionStatusDto>(sqlStatus, new { sedeId })).ToList();

                // 4. Endoso
                string sqlEndoso = @"
                    SELECT id, titulo_documento AS TituloDocumento, requiere_escaneado AS RequiereEscaneado,
                           requiere_info_beneficiario AS RequiereInfoBeneficiario, texto_parrafo_1 AS TextoParrafo1,
                           texto_parrafo_2 AS TextoParrafo2, texto_parrafo_3 AS TextoParrafo3, texto_parrafo_4 AS TextoParrafo4
                    FROM dbo.Configuracion_Facturacion_Endoso WHERE sede_id = @sedeId";
                dto.Endoso = await _db.QueryFirstOrDefaultAsync<FacturacionEndosoDto>(sqlEndoso, new { sedeId }) ?? new FacturacionEndosoDto();

                // 5 y 6. Cláusulas y Docs
                dto.ClausulasEndoso = (await _db.QueryAsync<FacturacionEndosoClausulaDto>(
                    "SELECT id, nombre, descripcion, activo FROM dbo.Configuracion_Facturacion_Endoso_Clausulas WHERE sede_id = @sedeId",
                    new { sedeId })).ToList();
                dto.DocumentosEndoso = (await _db.QueryAsync<FacturacionEndosoDocDto>(
                    "SELECT id, nombre, formato, obligatorio, activo FROM dbo.Configuracion_Facturacion_Endoso_Docs WHERE sede_id = @sedeId",
                    new { sedeId })).ToList();

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar la configuración", error = ex.Message });
            }
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarConfiguracion([FromBody] FacturacionConfigDto dto, [FromQuery] int sedeId)
        {
            if (dto == null) return BadRequest("Datos inválidos");
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var transaction = _db.BeginTransaction();
            try
            {
                // 1. Actualizar/Insertar General
                var existeGeneral = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.Configuracion_Facturacion_General WHERE sede_id = @sedeId",
                    new { sedeId }, transaction);

                if (existeGeneral > 0)
                {
                    var sqlGeneral = @"
                        UPDATE dbo.Configuracion_Facturacion_General SET
                            validar_rfc_sat = @ValidarRfcSat,
                            dias_alerta_vencimiento = @DiasAlertaVencimiento,
                            permitir_docs_vencidos = @PermitirDocsVencidos,
                            validar_formato_archivos = @ValidarFormatoArchivos,
                            tamano_maximo_mb = @TamanoMaximoMb,
                            retencion_federal_pct = @RetencionFederalPct,
                            retencion_isr_pct = @RetencionIsrPct,
                            retencion_iva_pct = @RetencionIvaPct,
                            aplicar_retencion_auto = @AplicarRetencionAuto,
                            longitud_rfc_fisica = @LongitudRfcFisica,
                            longitud_rfc_moral = @LongitudRfcMoral,
                            requiere_acta_moral = @RequiereActaMoral,
                            formatos_aceptados = @FormatosAceptados
                        WHERE sede_id = @SedeId";
                    await _db.ExecuteAsync(sqlGeneral, new { dto.General.ValidarRfcSat, dto.General.DiasAlertaVencimiento,
                        dto.General.PermitirDocsVencidos, dto.General.ValidarFormatoArchivos, dto.General.TamanoMaximoMb,
                        dto.General.RetencionFederalPct, dto.General.RetencionIsrPct, dto.General.RetencionIvaPct,
                        dto.General.AplicarRetencionAuto, dto.General.LongitudRfcFisica, dto.General.LongitudRfcMoral,
                        dto.General.RequiereActaMoral, dto.General.FormatosAceptados, SedeId = sedeId }, transaction);
                }

                // 2. Documentos (solo actualizar si existen y pertenecen a la sede)
                if (dto.Documentos != null && dto.Documentos.Any())
                {
                    foreach (var doc in dto.Documentos)
                    {
                        await _db.ExecuteAsync(@"UPDATE dbo.Configuracion_Facturacion_Documentos SET
                                nombre = @Nombre, formato = @Formato, obligatorio = @Obligatorio,
                                requiere_vigencia = @RequiereVigencia, activo = @Activo
                                WHERE id = @Id AND sede_id = @SedeId",
                            new { doc.Nombre, doc.Formato, doc.Obligatorio, doc.RequiereVigencia, doc.Activo, doc.Id, SedeId = sedeId }, transaction);
                    }
                }

                // 3. Status
                if (dto.StatusFlujo != null && dto.StatusFlujo.Any())
                {
                    foreach (var status in dto.StatusFlujo)
                    {
                        await _db.ExecuteAsync(@"UPDATE dbo.Configuracion_Facturacion_Status SET
                                nombre = @Nombre, activo = @Activo
                                WHERE id = @Id AND sede_id = @SedeId",
                            new { status.Nombre, status.Activo, status.Id, SedeId = sedeId }, transaction);
                    }
                }

                // 4. Endoso
                var existeEndoso = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.Configuracion_Facturacion_Endoso WHERE sede_id = @sedeId",
                    new { sedeId }, transaction);

                if (existeEndoso > 0)
                {
                    await _db.ExecuteAsync(@"UPDATE dbo.Configuracion_Facturacion_Endoso SET
                            titulo_documento = @TituloDocumento, texto_parrafo_1 = @TextoParrafo1
                            WHERE sede_id = @SedeId",
                        new { dto.Endoso.TituloDocumento, dto.Endoso.TextoParrafo1, SedeId = sedeId }, transaction);
                }

                // 5 y 6. Listas rápidas
                if (dto.ClausulasEndoso != null && dto.ClausulasEndoso.Any())
                {
                    foreach (var clausula in dto.ClausulasEndoso)
                    {
                        await _db.ExecuteAsync(
                            "UPDATE dbo.Configuracion_Facturacion_Endoso_Clausulas SET activo = @Activo WHERE id = @Id AND sede_id = @SedeId",
                            new { clausula.Activo, clausula.Id, SedeId = sedeId }, transaction);
                    }
                }

                if (dto.DocumentosEndoso != null && dto.DocumentosEndoso.Any())
                {
                    foreach (var docEndoso in dto.DocumentosEndoso)
                    {
                        await _db.ExecuteAsync(
                            "UPDATE dbo.Configuracion_Facturacion_Endoso_Docs SET activo = @Activo WHERE id = @Id AND sede_id = @SedeId",
                            new { docEndoso.Activo, docEndoso.Id, SedeId = sedeId }, transaction);
                    }
                }

                transaction.Commit();
                return Ok(new { message = "Configuración guardada correctamente" });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "Error al guardar", error = ex.Message });
            }
        }
    }
}