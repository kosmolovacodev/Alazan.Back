using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Alazan.API.Models; // Asegúrate de que apunte a tu carpeta de modelos

namespace Alazan.API.Controllers
{
[ApiController]
// [Route("api/configuracion-recepcion")]
[Route("configuracion-recepcion")]

public class ConfiguracionRecepcionController : ControllerBase 
{
    private readonly IDbConnection _db;
    public ConfiguracionRecepcionController(IDbConnection db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetFullConfig([FromQuery] int sedeId) {
        // IMPORTANTE: Los alias coinciden con los nombres de tu DTO
        // Admin global (sedeId=0) ve configuración global
        // Usuarios normales ven configuración de su sede
        var reglas = await _db.QueryFirstOrDefaultAsync<ReglasRecepcionDto>(
            @"SELECT
                id AS Id,
                sede_id AS SedeId,
                factor_impurezas AS FactorImpurezas,
                asignacion_auto_silo AS AsignacionAutoSilo,
                regla_tipo_grano AS ReglaTipoGrano,
                regla_calibre AS ReglaCalibre,
                regla_exportacion AS ReglaExportacion,
                regla_capacidad AS ReglaCapacidad,
                alerta_capacidad_pct AS AlertaCapacidadPct,
                solicitar_aprobacion_prod AS SolicitarAprobacionProd,
                pregunta_productor AS PreguntaProductor,
                accion_si_acepta AS AccionSiAcepta,
                accion_si_rechaza AS AccionSiRechaza,
                solicitar_motivo_rechazo AS SolicitarMotivoRechazo,
                requerir_firma_digital AS RequerirFirmaDigital,
                val_atiende_morales AS ValAtiendeMorales,
                val_multiples_entregas AS ValMultiplesEntregas,
                val_bloquear_placas AS ValBloquearPlacas,
                val_lectura_auto_bascula AS ValLecturaAutoBascula,
                val_captura_manual_peso AS ValCapturaManualPeso,
                val_motivo_peso_manual AS ValMotivoPesoManual,
                tpl_ejidal AS TplEjidal,
                tpl_pequena_propiedad AS TplPequenaPropiedad,
                tpl_persona_moral AS TplPersonaMoral
            FROM Configuracion_Recepcion_Reglas
            WHERE sede_id = @sedeId",
            new { sedeId });

        var campos = await _db.QueryAsync<ConfiguracionCampoDto>(
            @"SELECT
                id AS Id,
                pantalla AS Pantalla,
                clave_campo AS ClaveCampo,
                nombre_mostrar AS NombreMostrar,
                orden AS Orden,
                visible AS Visible,
                obligatorio AS Obligatorio,
                es_sistema AS EsSistema,
                descripcion AS Descripcion
            FROM dbo.Configuracion_Campos_Pantallas
            WHERE sede_id = @sedeId
            ORDER BY pantalla, orden",
            new { sedeId });

        return Ok(new { reglas, campos });
    }

    [HttpPost("guardar-reglas")]
    public async Task<IActionResult> GuardarReglas([FromBody] ReglasRecepcionDto dto, [FromQuery] int sedeId)
    {
        try
        {
            // Capturar el email del usuario del header para auditoría
            var usuarioEmail = Request.Headers["X-User-Email"].ToString();
            if (string.IsNullOrEmpty(usuarioEmail))
            {
                usuarioEmail = "sistema"; // Valor por defecto si no viene el header
            }
            // Verificar si ya existe configuración para esta sede
            var existe = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.Configuracion_Recepcion_Reglas WHERE sede_id = @sedeId",
                new { sedeId });

            if (existe > 0)
            {
                // UPDATE: Actualizar configuración existente
                var sql = @"UPDATE dbo.Configuracion_Recepcion_Reglas SET
                            factor_impurezas = @FactorImpurezas,
                            asignacion_auto_silo = @AsignacionAutoSilo,
                            regla_tipo_grano = @ReglaTipoGrano,
                            regla_calibre = @ReglaCalibre,
                            regla_exportacion = @ReglaExportacion,
                            regla_capacidad = @ReglaCapacidad,
                            alerta_capacidad_pct = @AlertaCapacidadPct,
                            solicitar_aprobacion_prod = @SolicitarAprobacionProd,
                            pregunta_productor = @PreguntaProductor,
                            accion_si_acepta = @AccionSiAcepta,
                            accion_si_rechaza = @AccionSiRechaza,
                            solicitar_motivo_rechazo = @SolicitarMotivoRechazo,
                            requerir_firma_digital = @RequerirFirmaDigital,
                            val_atiende_morales = @ValAtiendeMorales,
                            val_multiples_entregas = @ValMultiplesEntregas,
                            val_bloquear_placas = @ValBloquearPlacas,
                            val_lectura_auto_bascula = @ValLecturaAutoBascula,
                            val_captura_manual_peso = @ValCapturaManualPeso,
                            val_motivo_peso_manual = @ValMotivoPesoManual,
                            tpl_ejidal = @TplEjidal,
                            tpl_pequena_propiedad = @TplPequenaPropiedad,
                            tpl_persona_moral = @TplPersonaMoral,
                            usuario_modificacion = @UsuarioModificacion,
                            fecha_modificacion = SYSDATETIMEOFFSET()
                            WHERE sede_id = @SedeId";

                await _db.ExecuteAsync(sql, new { dto.FactorImpurezas, dto.AsignacionAutoSilo, dto.ReglaTipoGrano,
                    dto.ReglaCalibre, dto.ReglaExportacion, dto.ReglaCapacidad, dto.AlertaCapacidadPct,
                    dto.SolicitarAprobacionProd, dto.PreguntaProductor, dto.AccionSiAcepta, dto.AccionSiRechaza,
                    dto.SolicitarMotivoRechazo, dto.RequerirFirmaDigital, dto.ValAtiendeMorales,
                    dto.ValMultiplesEntregas, dto.ValBloquearPlacas, dto.ValLecturaAutoBascula,
                    dto.ValCapturaManualPeso, dto.ValMotivoPesoManual, dto.TplEjidal, dto.TplPequenaPropiedad,
                    dto.TplPersonaMoral, UsuarioModificacion = usuarioEmail, SedeId = sedeId });
            }
            else
            {
                // INSERT: Crear nueva configuración para la sede
                // IMPORTANTE: Calculamos el siguiente ID porque la columna id NO es IDENTITY
                var nextId = await _db.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.Configuracion_Recepcion_Reglas");

                var sql = @"INSERT INTO dbo.Configuracion_Recepcion_Reglas
                            (id, sede_id, factor_impurezas, asignacion_auto_silo, regla_tipo_grano, regla_calibre,
                            regla_exportacion, regla_capacidad, alerta_capacidad_pct, solicitar_aprobacion_prod,
                            pregunta_productor, accion_si_acepta, accion_si_rechaza, solicitar_motivo_rechazo,
                            requerir_firma_digital, val_atiende_morales, val_multiples_entregas, val_bloquear_placas,
                            val_lectura_auto_bascula, val_captura_manual_peso, val_motivo_peso_manual,
                            tpl_ejidal, tpl_pequena_propiedad, tpl_persona_moral, usuario_modificacion, fecha_modificacion)
                            VALUES
                            (@Id, @SedeId, @FactorImpurezas, @AsignacionAutoSilo, @ReglaTipoGrano, @ReglaCalibre,
                            @ReglaExportacion, @ReglaCapacidad, @AlertaCapacidadPct, @SolicitarAprobacionProd,
                            @PreguntaProductor, @AccionSiAcepta, @AccionSiRechaza, @SolicitarMotivoRechazo,
                            @RequerirFirmaDigital, @ValAtiendeMorales, @ValMultiplesEntregas, @ValBloquearPlacas,
                            @ValLecturaAutoBascula, @ValCapturaManualPeso, @ValMotivoPesoManual,
                            @TplEjidal, @TplPequenaPropiedad, @TplPersonaMoral, @UsuarioModificacion, SYSDATETIMEOFFSET())";

                await _db.ExecuteAsync(sql, new { Id = nextId, SedeId = sedeId, dto.FactorImpurezas, dto.AsignacionAutoSilo,
                    dto.ReglaTipoGrano, dto.ReglaCalibre, dto.ReglaExportacion, dto.ReglaCapacidad,
                    dto.AlertaCapacidadPct, dto.SolicitarAprobacionProd, dto.PreguntaProductor, dto.AccionSiAcepta,
                    dto.AccionSiRechaza, dto.SolicitarMotivoRechazo, dto.RequerirFirmaDigital, dto.ValAtiendeMorales,
                    dto.ValMultiplesEntregas, dto.ValBloquearPlacas, dto.ValLecturaAutoBascula,
                    dto.ValCapturaManualPeso, dto.ValMotivoPesoManual, dto.TplEjidal, dto.TplPequenaPropiedad,
                    dto.TplPersonaMoral, UsuarioModificacion = usuarioEmail });
            }

            return Ok(new { message = "Configuración guardada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno", error = ex.Message });
        }
    }

    [HttpPost("campos")]
    public async Task<IActionResult> CrearCampo([FromBody] ConfiguracionCampoDto dto, [FromQuery] int sedeId)
    {
        try
        {
            // Calculamos el siguiente orden para esa pantalla específica y sede
            var sqlOrden = @"SELECT ISNULL(MAX(orden), 0) + 1
                            FROM dbo.Configuracion_Campos_Pantallas
                            WHERE pantalla = @Pantalla AND sede_id = @sedeId";
            dto.Orden = await _db.ExecuteScalarAsync<int>(sqlOrden, new { dto.Pantalla, sedeId });

            dto.EsSistema = false; // Forzado ya que es manual

            var sql = @"INSERT INTO dbo.Configuracion_Campos_Pantallas
                        (pantalla, clave_campo, nombre_mostrar, orden, visible,
                        obligatorio, es_sistema, sede_id)
                        VALUES
                        (@Pantalla, @ClaveCampo, @NombreMostrar, @Orden, @Visible,
                        @Obligatorio, @EsSistema, @SedeId);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

            dto.Id = await _db.QuerySingleAsync<int>(sql, new
            {
                dto.Pantalla,
                dto.ClaveCampo,
                dto.NombreMostrar,
                dto.Orden,
                dto.Visible,
                dto.Obligatorio,
                dto.EsSistema,
                SedeId = sedeId
            });
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear campo", error = ex.Message });
        }
    }

    // 2. ACTUALIZAR TODOS LOS CAMPOS (Al presionar Guardar Cambios general)
    [HttpPut("actualizar-lista-campos")]
    public async Task<IActionResult> ActualizarListaCampos([FromBody] List<ConfiguracionCampoDto> campos, [FromQuery] int sedeId)
    {
        if (campos == null || !campos.Any()) return BadRequest("Lista vacía");

        try
        {
            // Validar que todos los campos pertenecen a la sede del usuario
            if (sedeId != 0)
            {
                var ids = string.Join(",", campos.Select(c => c.Id));
                var camposOtraSede = await _db.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM dbo.Configuracion_Campos_Pantallas WHERE id IN ({ids}) AND sede_id != @sedeId",
                    new { sedeId });

                if (camposOtraSede > 0)
                {
                    return StatusCode(403, new { message = "No tiene permiso para modificar campos de otra sede" });
                }
            }

            // Usamos la conexión que ya tienes inyectada
            if (_db.State == ConnectionState.Closed) _db.Open();

            using var trans = _db.BeginTransaction();

            // El SQL debe usar los nombres de columnas de tu tabla
            // y los parámetros @Propiedad deben coincidir con tu DTO
            var sql = @"
                UPDATE dbo.Configuracion_Campos_Pantallas
                SET
                    orden = @Orden,
                    visible = CASE WHEN @Visible = 1 THEN 1 ELSE 0 END,
                    obligatorio = CASE WHEN @Obligatorio = 1 THEN 1 ELSE 0 END,
                    nombre_mostrar = @NombreMostrar
                WHERE id = @Id";

            // Ejecutamos el update masivo
            await _db.ExecuteAsync(sql, campos, transaction: trans);

            trans.Commit();
            return Ok(new { message = "Cambios guardados en base de datos" });
        }
        catch (Exception ex)
        {
            // Si algo falla, mostramos el error real en la consola de VS
            Console.WriteLine("ERROR UPDATE: " + ex.Message);
            return StatusCode(500, new { message = "Error interno", error = ex.Message });
        }
    }

    [HttpDelete("campos/{id}")]
    public async Task<IActionResult> EliminarCampo(int id, [FromQuery] int sedeId)
    {
        try
        {
            // 1. Verificamos que el campo exista y que NO sea de sistema
            var campo = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT es_sistema AS EsSistema, sede_id AS SedeId FROM dbo.Configuracion_Campos_Pantallas WHERE id = @Id",
                new { Id = id });

            if (campo == null) return NotFound("El campo no existe");
            if (campo.EsSistema) return BadRequest("No se pueden eliminar campos del sistema");

            // 2. Validar que pertenece a la sede del usuario
            if (sedeId != 0 && campo.SedeId != sedeId)
            {
                return StatusCode(403, new { message = "No tiene permiso para eliminar campos de otra sede" });
            }

            // 3. Eliminamos
            await _db.ExecuteAsync("DELETE FROM dbo.Configuracion_Campos_Pantallas WHERE id = @Id", new { Id = id });

            return Ok(new { message = "Campo eliminado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar el campo", error = ex.Message });
        }
    }


}
}