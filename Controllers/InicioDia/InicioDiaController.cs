using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("inicio-dia")]
    public class InicioDiaController : ControllerBase
    {
        private readonly IDbConnection _db;
        public InicioDiaController(IDbConnection db) => _db = db;

        // ─────────────────────────────────────────────────────────────────
        // GET /inicio-dia/secciones  — catálogo de secciones configuradas
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("secciones")]
        public async Task<IActionResult> GetSecciones()
        {
            var rows = await _db.QueryAsync(
                "SELECT codigo, nombre, icono, color, activo, orden FROM dbo.inicio_dia_secciones ORDER BY orden");
            return Ok(rows);
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /inicio-dia/secciones  — crear una nueva sección
        // ─────────────────────────────────────────────────────────────────
        [HttpPost("secciones")]
        public async Task<IActionResult> CreateSeccion([FromBody] SeccionCreateRequest r)
        {
            var exists = await _db.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM dbo.inicio_dia_secciones WHERE codigo = @codigo",
                new { codigo = r.Codigo });
            if (exists > 0)
                return Conflict(new { message = $"Ya existe una sección con código '{r.Codigo}'" });

            await _db.ExecuteAsync(
                @"INSERT INTO dbo.inicio_dia_secciones (codigo, nombre, icono, color, activo, orden)
                  VALUES (@codigo, @nombre, @icono, @color, @activo, @orden)",
                new { codigo = r.Codigo, nombre = r.Nombre, icono = r.Icono, color = r.Color, activo = r.Activo, orden = r.Orden });
            return Ok(new { codigo = r.Codigo });
        }

        // ─────────────────────────────────────────────────────────────────
        // PUT /inicio-dia/secciones/{codigo}  — actualizar una sección
        // Si r.NuevoCodigo != codigo se renombra la PK (DELETE + INSERT)
        // ─────────────────────────────────────────────────────────────────
        [HttpPut("secciones/{codigo}")]
        public async Task<IActionResult> UpdateSeccion(string codigo, [FromBody] SeccionUpdateRequest r)
        {
            // Normalizar a mayúsculas para evitar falsas diferencias por case
            codigo = codigo.ToUpper();
            var nuevoCodigo = string.IsNullOrWhiteSpace(r.NuevoCodigo) ? codigo : r.NuevoCodigo.Trim().ToUpper();

            if (_db.State == ConnectionState.Closed) _db.Open();
            using var tx = _db.BeginTransaction();
            try
            {
                if (nuevoCodigo != codigo)
                {
                    var dup = await _db.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(1) FROM dbo.inicio_dia_secciones WHERE codigo = @nuevoCodigo",
                        new { nuevoCodigo }, tx);
                    if (dup > 0)
                    {
                        tx.Rollback();
                        return Conflict(new { message = $"Ya existe una sección con código '{nuevoCodigo}'" });
                    }

                    await _db.ExecuteAsync(
                        @"INSERT INTO dbo.inicio_dia_secciones (codigo, nombre, icono, color, activo, orden)
                          VALUES (@nuevoCodigo, @nombre, @icono, @color, @activo, @orden)",
                        new { nuevoCodigo, nombre = r.Nombre, icono = r.Icono, color = r.Color, activo = r.Activo, orden = r.Orden }, tx);
                    await _db.ExecuteAsync(
                        "DELETE FROM dbo.inicio_dia_secciones WHERE codigo = @codigo",
                        new { codigo }, tx);
                }
                else
                {
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.inicio_dia_secciones
                          SET nombre = @nombre, icono = @icono, color = @color, activo = @activo, orden = @orden
                          WHERE codigo = @codigo",
                        new { codigo, nombre = r.Nombre, icono = r.Icono, color = r.Color, activo = r.Activo, orden = r.Orden }, tx);
                }

                tx.Commit();
                return Ok(new { codigo = nuevoCodigo });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return StatusCode(500, new { message = ex.Message });
            }
            finally { _db.Close(); }
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE /inicio-dia/secciones/{codigo}  — eliminar una sección
        // ─────────────────────────────────────────────────────────────────
        [HttpDelete("secciones/{codigo}")]
        public async Task<IActionResult> DeleteSeccion(string codigo)
        {
            await _db.ExecuteAsync(
                "DELETE FROM dbo.inicio_dia_secciones WHERE codigo = @codigo",
                new { codigo });
            return Ok();
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /inicio-dia/estado?sedeId=X&fecha=YYYY-MM-DD
        // Devuelve el estado de cada sección para la fecha indicada
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("estado")]
        public async Task<IActionResult> GetEstado([FromQuery] int sedeId, [FromQuery] string? fecha = null)
        {
            var dia = fecha != null ? DateTime.Parse(fecha) : DateTime.Today;

            var secciones = await _db.QueryAsync(
                @"SELECT s.codigo, s.nombre, s.icono, s.color,
                         d.completo, d.iniciado_por,
                         d.created_at AS hora_inicio,
                         u.nombre_completo AS iniciado_por_nombre
                  FROM dbo.inicio_dia_secciones s
                  LEFT JOIN dbo.inicio_dia_dias d
                         ON d.sede_id = @sedeId AND d.fecha = @dia AND d.seccion = s.codigo
                  LEFT JOIN dbo.usuarios u ON u.id = d.iniciado_por
                  WHERE s.activo = 1
                  ORDER BY s.orden",
                new { sedeId, dia });

            var asistencias = await _db.QueryAsync(
                @"SELECT a.usuario_id, u.nombre_completo AS nombre,
                         r.seccion_inicio_dia AS seccion,
                         r.tipo_inicio_dia,
                         a.asistio, a.created_at AS hora,
                         reg.nombre_completo AS registrado_por_nombre
                  FROM dbo.inicio_dia_asistencias a
                  JOIN dbo.usuarios u   ON u.id = a.usuario_id
                  JOIN dbo.roles    r   ON r.id = u.rol_id
                  JOIN dbo.usuarios reg ON reg.id = a.registrado_por
                  WHERE a.sede_id = @sedeId AND a.fecha = @dia
                  ORDER BY r.seccion_inicio_dia, u.nombre_completo",
                new { sedeId, dia });

            return Ok(new { fecha = dia.ToString("yyyy-MM-dd"), secciones, asistencias });
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE /inicio-dia/reset?sedeId=X&fecha=YYYY-MM-DD
        // Reinicia el día (solo admin): borra días y asistencias de la fecha
        // ─────────────────────────────────────────────────────────────────
        [HttpDelete("reset")]
        public async Task<IActionResult> Reset([FromQuery] int sedeId, [FromQuery] string fecha)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var tx = _db.BeginTransaction();
            try
            {
                var dia = DateTime.Parse(fecha);
                await _db.ExecuteAsync("DELETE FROM dbo.inicio_dia_dias         WHERE sede_id=@sedeId AND fecha=@dia", new { sedeId, dia }, tx);
                await _db.ExecuteAsync("DELETE FROM dbo.inicio_dia_asistencias  WHERE sede_id=@sedeId AND fecha=@dia", new { sedeId, dia }, tx);
                tx.Commit();
                return Ok(new { message = "Día reiniciado correctamente" });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return StatusCode(500, new { message = ex.Message });
            }
            finally { _db.Close(); }
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/inicio-dia/hoy?sedeId=X
        //
        // Devuelve:
        //   - completo: bool  (true = ya fue iniciado hoy)
        //   - fecha
        //   - personal: usuarios agrupados por sección, derivada de
        //               departamento o del nombre del rol
        //   - asistencias: registros ya guardados para hoy
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("hoy")]
        public async Task<IActionResult> GetHoy([FromQuery] int sedeId, [FromQuery] string? seccion = null)
        {
            try
            {
                var hoy = DateTime.Today;

                // ¿La sección (o el día completo) ya fue iniciada?
                // Si se pasa seccion: busca esa sección O un inicio global (seccion IS NULL).
                // Si no se pasa: busca cualquier registro completo del día.
                var completo = await _db.QueryFirstOrDefaultAsync<bool>(
                    @"SELECT TOP 1 1
                      FROM dbo.inicio_dia_dias
                      WHERE sede_id = @sedeId AND fecha = @hoy AND completo = 1
                        AND (@seccion IS NULL OR seccion = @seccion OR seccion IS NULL)",
                    new { sedeId, hoy, seccion });

                if (completo)
                    return Ok(new { completo = true, fecha = hoy.ToString("yyyy-MM-dd") });

                // Roles que participan en inicio de día (definen las "posiciones" a cubrir)
                var rolesSeccion = await _db.QueryAsync(
                    @"SELECT id AS rol_id, nombre_rol,
                             seccion_inicio_dia AS seccion,
                             tipo_inicio_dia,
                             CASE WHEN tipo_inicio_dia = 'SUPERVISOR' THEN 1 ELSE 0 END AS es_supervisor
                      FROM dbo.roles
                      WHERE seccion_inicio_dia IS NOT NULL
                        AND activo = 1
                        AND (sede_id = @sedeId OR sede_id = 0 OR sede_id IS NULL)
                      ORDER BY seccion_inicio_dia, es_supervisor DESC, nombre_rol",
                    new { sedeId });

                // Personal de la sede disponible para asignar a cada posición
                var personal = await _db.QueryAsync(
                    @"SELECT
                          u.id,
                          u.nombre_completo AS nombre,
                          r.nombre_rol,
                          r.seccion_inicio_dia AS seccion,
                          r.id AS rol_id,
                          CASE WHEN r.tipo_inicio_dia = 'SUPERVISOR' THEN 1 ELSE 0 END AS es_supervisor
                      FROM dbo.usuarios u
                      INNER JOIN dbo.roles r ON u.rol_id = r.id
                      WHERE u.activo = 1
                        AND (u.sede_id = @sedeId OR u.sede_id = 0)
                        AND r.seccion_inicio_dia IS NOT NULL
                      ORDER BY r.seccion_inicio_dia, es_supervisor DESC, u.nombre_completo",
                    new { sedeId });

                // Asistencias ya registradas hoy
                var asistencias = await _db.QueryAsync(
                    @"SELECT usuario_id, asistio
                      FROM dbo.inicio_dia_asistencias
                      WHERE sede_id = @sedeId AND fecha = @hoy",
                    new { sedeId, hoy });

                return Ok(new
                {
                    completo = false,
                    fecha = hoy.ToString("yyyy-MM-dd"),
                    roles_seccion = rolesSeccion,
                    personal,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /api/inicio-dia/guardar
        //
        // Body: {
        //   sedeId, registradoPor, marcarCompleto,
        //   asistencias: [{ usuarioId, asistio }]
        // }
        // ─────────────────────────────────────────────────────────────────
        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] GuardarInicioDiaRequest req)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var transaction = _db.BeginTransaction();
            try
            {
                var hoy = DateTime.Today;

                foreach (var a in req.Asistencias)
                {
                    await _db.ExecuteAsync(
                        @"MERGE dbo.inicio_dia_asistencias AS target
                          USING (SELECT @sedeId AS sede_id, @fecha AS fecha, @usuarioId AS usuario_id) AS source
                          ON target.sede_id   = source.sede_id
                         AND target.fecha     = source.fecha
                         AND target.usuario_id = source.usuario_id
                          WHEN MATCHED THEN
                              UPDATE SET asistio = @asistio, registrado_por = @registradoPor
                          WHEN NOT MATCHED THEN
                              INSERT (sede_id, fecha, usuario_id, asistio, registrado_por)
                              VALUES (@sedeId, @fecha, @usuarioId, @asistio, @registradoPor);",
                        new
                        {
                            sedeId       = req.SedeId,
                            fecha        = hoy,
                            usuarioId    = a.UsuarioId,
                            asistio      = a.Asistio,
                            registradoPor = req.RegistradoPor
                        },
                        transaction);
                }

                if (req.MarcarCompleto)
                {
                    await _db.ExecuteAsync(
                        @"MERGE dbo.inicio_dia_dias AS target
                          USING (SELECT @sedeId AS sede_id, @fecha AS fecha, @seccion AS seccion) AS source
                          ON  target.sede_id = source.sede_id
                          AND target.fecha   = source.fecha
                          AND (
                                (target.seccion IS NULL AND source.seccion IS NULL)
                                OR target.seccion = source.seccion
                              )
                          WHEN MATCHED THEN
                              UPDATE SET completo = 1, iniciado_por = @iniciadoPor
                          WHEN NOT MATCHED THEN
                              INSERT (sede_id, fecha, completo, iniciado_por, seccion)
                              VALUES (@sedeId, @fecha, 1, @iniciadoPor, @seccion);",
                        new { sedeId = req.SedeId, fecha = hoy, iniciadoPor = req.RegistradoPor, seccion = req.Seccion },
                        transaction);
                }

                transaction.Commit();
                return Ok(new
                {
                    message = req.MarcarCompleto ? "Día iniciado correctamente" : "Asistencias guardadas"
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
            finally
            {
                _db.Close();
            }
        }
    }

    public class GuardarInicioDiaRequest
    {
        public int SedeId { get; set; }
        public int RegistradoPor { get; set; }
        public bool MarcarCompleto { get; set; }
        public string? Seccion { get; set; }
        public List<AsistenciaDto> Asistencias { get; set; } = new();
    }

    public class AsistenciaDto
    {
        public int UsuarioId { get; set; }
        public bool Asistio { get; set; }
    }

    public class SeccionUpdateRequest
    {
        public string? NuevoCodigo { get; set; }
        public string Nombre { get; set; } = "";
        public string Icono { get; set; } = "";
        public string Color { get; set; } = "";
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class SeccionCreateRequest
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Icono { get; set; } = "place";
        public string Color { get; set; } = "#607D8B";
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
    }
}
