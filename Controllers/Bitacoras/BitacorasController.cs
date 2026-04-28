using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Text.Json;
using System.Security.Claims;
using Dapper;
using Alazan.API.Services;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("bitacoras")]
    public class BitacorasController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly EmailService _email;
        private readonly BitacoraPdfService _pdf;
        private readonly IConfiguration _config;

        public BitacorasController(IDbConnection db, EmailService email,
            BitacoraPdfService pdf, IConfiguration config)
        {
            _db = db; _email = email; _pdf = pdf; _config = config;
        }

        // ─── GET /bitacoras/secciones?sedeId=X ───────────────────────
        [HttpGet("secciones")]
        public async Task<IActionResult> GetSecciones([FromQuery] int sedeId)
        {
            var stats = await _db.QueryAsync(
                @"SELECT seccion_codigo AS seccionCodigo,
                         SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente
                   FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  GROUP BY seccion_codigo",
                new { SedeId = sedeId });

            var statsBySec = stats.ToDictionary(
                r => (string)r.seccionCodigo,
                r => (int)r.pendiente);

            var secciones = await _db.QueryAsync(
                @"SELECT
                    s.codigo,
                    s.nombre,
                    s.descripcion,
                    s.icono,
                    s.color,
                    s.orden,
                    s.roles_acceso  AS rolesAcceso,
                    COUNT(d.codigo) AS totalBitacoras
                  FROM dbo.bitacoras_secciones s
                  LEFT JOIN dbo.bitacoras_definicion d
                    ON d.seccion_codigo = s.codigo AND d.activo = 1
                  WHERE s.activo = 1
                  GROUP BY s.codigo, s.nombre, s.descripcion, s.icono, s.color, s.orden, s.roles_acceso
                  ORDER BY s.orden");

            var result = secciones.Select(s => new
            {
                s.codigo,
                s.nombre,
                descripcion  = (string?)s.descripcion,
                s.icono,
                s.color,
                s.orden,
                rolesAcceso  = (string?)s.rolesAcceso,
                totalBitacoras = (int)s.totalBitacoras,
                pendiente = statsBySec.TryGetValue((string)s.codigo, out var p) ? p : 0,
            });

            return Ok(result);
        }

        // ─── GET /bitacoras/definicion/{seccion}?sedeId=X ────────────
        [HttpGet("definicion/{seccion}")]
        public async Task<IActionResult> GetDefinicion(string seccion, [FromQuery] int sedeId)
        {
            var bitacoras = await _db.QueryAsync(
                @"SELECT
                    d.codigo,
                    d.nombre,
                    d.tipo,
                    d.fuente_query AS fuenteQuery,
                    d.orden,
                    ISNULL(stats.pendiente, 0) AS pendiente
                  FROM dbo.bitacoras_definicion d
                  LEFT JOIN (
                      SELECT codigo_bitacora,
                             SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente
                        FROM dbo.bitacoras_registros
                       WHERE activo = 1
                         AND (@SedeId = 0 OR sede_id = @SedeId)
                       GROUP BY codigo_bitacora
                  ) stats ON stats.codigo_bitacora = d.codigo
                  WHERE d.seccion_codigo = @Seccion
                    AND d.activo = 1
                  ORDER BY d.orden",
                new { Seccion = seccion, SedeId = sedeId });

            return Ok(bitacoras);
        }

        // ─── GET /bitacoras/columnas/{codigo} ────────────────────────
        [HttpGet("columnas/{codigo}")]
        public async Task<IActionResult> GetColumnas(string codigo)
        {
            var columnas = await _db.QueryAsync(
                @"SELECT
                    campo,
                    label,
                    tipo_dato AS tipoDato,
                    es_meta   AS esMeta,
                    orden
                  FROM dbo.bitacoras_columnas
                  WHERE codigo_bitacora = @Codigo
                    AND visible = 1
                  ORDER BY orden",
                new { Codigo = codigo });

            return Ok(columnas);
        }

        // ─── GET /bitacoras/stats?sedeId=X ───────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int sedeId)
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    codigo_bitacora   AS codigoBitacora,
                    seccion_codigo    AS seccionCodigo,
                    COUNT(*)          AS total,
                    SUM(CASE WHEN status = 'Pendiente' THEN 1 ELSE 0 END) AS pendiente,
                    SUM(CASE WHEN status = 'Firmada'   THEN 1 ELSE 0 END) AS firmada,
                    SUM(CASE WHEN status = 'Impresa'   THEN 1 ELSE 0 END) AS impresa
                  FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  GROUP BY codigo_bitacora, seccion_codigo",
                new { SedeId = sedeId });
            return Ok(rows);
        }

        // ─── GET /bitacoras/token/{token} — PÚBLICO (página de firma) ──
        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetInfoToken(string token)
        {
            var slot = await _db.QueryFirstOrDefaultAsync(
                @"SELECT
                    bf.id,
                    bf.usuario_id       AS usuarioId,
                    bf.nombre_firmante  AS nombreFirmante,
                    bf.token_usado      AS tokenUsado,
                    bf.token_expira     AS tokenExpira,
                    bf.firmado_en       AS firmadoEn,
                    bf.rol_requerido    AS rolRequerido,
                    br.codigo_bitacora  AS codigoBitacora,
                    bd.nombre           AS nombreBitacora,
                    br.fecha,
                    bs.nombre           AS seccion
                  FROM dbo.bitacoras_firmas bf
                  JOIN dbo.bitacoras_registros br ON br.id = bf.registro_id
                  JOIN dbo.bitacoras_definicion bd ON bd.codigo = br.codigo_bitacora
                  JOIN dbo.bitacoras_secciones bs  ON bs.codigo = br.seccion_codigo
                  WHERE bf.token_firma = @Token",
                new { Token = token });

            if (slot == null) return NotFound(new { message = "Token inválido o no encontrado" });

            return Ok(new
            {
                slot.id,
                slot.nombreFirmante,
                slot.rolRequerido,
                slot.tokenUsado,
                slot.firmadoEn,
                expirado = slot.tokenExpira != null && slot.tokenExpira < DateTime.Now,
                slot.nombreBitacora,
                slot.codigoBitacora,
                slot.seccion,
                fecha = slot.fecha,
            });
        }

        // ─── POST /bitacoras/firmar — PÚBLICO (firmar con token + NIP) ──
        [HttpPost("firmar")]
        public async Task<IActionResult> FirmarConToken([FromBody] FirmarRequest req)
        {
            var slot = await _db.QueryFirstOrDefaultAsync(
                @"SELECT
                    bf.id,
                    bf.usuario_id    AS usuarioId,
                    bf.token_usado   AS tokenUsado,
                    bf.token_expira  AS tokenExpira,
                    bf.registro_id   AS registroId,
                    bf.nombre_firmante AS nombreFirmante,
                    br.firmas_requeridas   AS firmasRequeridas,
                    br.firmas_completadas  AS firmasCompletadas
                  FROM dbo.bitacoras_firmas bf
                  JOIN dbo.bitacoras_registros br ON br.id = bf.registro_id
                  WHERE bf.token_firma = @Token",
                new { req.Token });

            if (slot == null)
                return NotFound(new { message = "Token inválido" });
            if ((bool)slot.tokenUsado)
                return BadRequest(new { message = "Este enlace ya fue utilizado" });
            if (slot.tokenExpira != null && slot.tokenExpira < DateTime.Now)
                return BadRequest(new { message = "Este enlace ha expirado" });

            // Validar NIP
            var usuario = await _db.QueryFirstOrDefaultAsync(
                "SELECT nip_hash FROM dbo.usuarios WHERE id = @Id AND activo = 1",
                new { Id = (long)slot.usuarioId });

            if (usuario == null || string.IsNullOrEmpty((string?)usuario.nip_hash))
                return BadRequest(new { message = "El usuario no tiene NIP configurado" });

            if (!BCrypt.Net.BCrypt.Verify(req.Nip, (string)usuario.nip_hash))
                return BadRequest(new { message = "NIP incorrecto", nipInvalido = true });

            // Registrar firma
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            if (_db.State == ConnectionState.Closed) _db.Open();
            using var trans = _db.BeginTransaction();
            try
            {
                await _db.ExecuteAsync(
                    @"UPDATE dbo.bitacoras_firmas
                         SET token_usado = 1, firmado_en = GETDATE(), ip_firma = @Ip
                       WHERE id = @Id",
                    new { Ip = ip, Id = (int)slot.id }, transaction: trans);

                var nuevasCompletadas = (int)slot.firmasCompletadas + 1;
                var nuevoStatus = nuevasCompletadas >= (int)slot.firmasRequeridas ? "Firmada" : null;

                if (nuevoStatus != null)
                {
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.bitacoras_registros
                             SET firmas_completadas = @Completadas, status = @Status
                           WHERE id = @Id",
                        new { Completadas = nuevasCompletadas, Status = nuevoStatus, Id = (int)slot.registroId },
                        transaction: trans);
                }
                else
                {
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.bitacoras_registros
                             SET firmas_completadas = @Completadas
                           WHERE id = @Id",
                        new { Completadas = nuevasCompletadas, Id = (int)slot.registroId },
                        transaction: trans);
                }

                trans.Commit();
                return Ok(new { ok = true, mensaje = "Tu firma ha sido registrada exitosamente" });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, new { message = "Error al registrar firma", error = ex.Message });
            }
        }

        // ─── GET /bitacoras/{codigo}?sedeId=X ────────────────────────
        [HttpGet("{codigo}")]
        public async Task<IActionResult> GetRegistros(string codigo, [FromQuery] int sedeId)
        {
            var def = await _db.QueryFirstOrDefaultAsync(
                @"SELECT tipo, fuente_query AS fuenteQuery
                    FROM dbo.bitacoras_definicion
                   WHERE codigo = @Codigo AND activo = 1",
                new { Codigo = codigo });

            if (def == null)
                return NotFound(new { message = $"Bitácora '{codigo}' no encontrada" });

            string tipo = (string)def.tipo;
            string? fuenteQuery = (string?)def.fuenteQuery;

            // ── LINKED: datos vienen de vista operacional ─────────────
            if (tipo == "linked" && !string.IsNullOrWhiteSpace(fuenteQuery))
            {
                if (!fuenteQuery.StartsWith("vw_bitacora_", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Nombre de vista inválido" });

                var sql = $@"SELECT * FROM dbo.{fuenteQuery}
                             WHERE (@SedeId = 0 OR sede_id = @SedeId)
                             ORDER BY fecha DESC, id DESC";

                var rows = await _db.QueryAsync(sql, new { SedeId = sedeId });

                var result = rows.Select(r =>
                {
                    var dict = (IDictionary<string, object?>)r;
                    var datos = new Dictionary<string, object?>();
                    foreach (var kv in dict)
                    {
                        var key = kv.Key.ToLowerInvariant();
                        if (key is "id" or "sede_id" or "fecha" or "status") continue;
                        datos[key] = kv.Value;
                    }
                    return new
                    {
                        id       = dict.TryGetValue("id", out var id) ? id : null,
                        codigoBitacora = codigo,
                        fecha    = dict.TryGetValue("fecha", out var f) ? f : null,
                        status   = dict.TryGetValue("status", out var s) ? s : "Pendiente",
                        datos,
                        sedeId,
                        pdfPath           = (object?)null,
                        firmasRequeridas  = 0,
                        firmasCompletadas = 0,
                    };
                });

                return Ok(result);
            }

            // ── MANUAL: datos vienen de bitacoras_registros ───────────
            var manualRows = await _db.QueryAsync(
                @"SELECT
                    id,
                    codigo_bitacora    AS codigoBitacora,
                    seccion_codigo     AS seccionCodigo,
                    CONVERT(DATE, fecha) AS fecha,
                    status,
                    datos_json         AS datosJson,
                    sede_id            AS sedeId,
                    created_at         AS creadoEn,
                    pdf_path           AS pdfPath,
                    firmas_requeridas  AS firmasRequeridas,
                    firmas_completadas AS firmasCompletadas
                  FROM dbo.bitacoras_registros
                  WHERE activo = 1
                    AND codigo_bitacora = @Codigo
                    AND (@SedeId = 0 OR sede_id = @SedeId)
                  ORDER BY fecha DESC, id DESC",
                new { Codigo = codigo, SedeId = sedeId });

            var manualResult = manualRows.Select(r => new
            {
                r.id,
                r.codigoBitacora,
                r.seccionCodigo,
                r.fecha,
                r.status,
                datos = r.datosJson != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(r.datosJson)
                    : new Dictionary<string, object>(),
                r.sedeId,
                r.creadoEn,
                r.pdfPath,
                r.firmasRequeridas,
                r.firmasCompletadas,
            });

            return Ok(manualResult);
        }

        // ─── POST /bitacoras/{codigo} ─────────────────────────────────
        [HttpPost("{codigo}")]
        public async Task<IActionResult> Crear(string codigo, [FromBody] BitacoraRegistroRequest dto)
        {
            try
            {
                var datosJson = JsonSerializer.Serialize(dto.Datos);
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.bitacoras_registros
                        (codigo_bitacora, seccion_codigo, fecha, status, datos_json, sede_id, activo, created_at)
                      VALUES
                        (@Codigo, @SeccionCodigo, @Fecha, 'Pendiente', @DatosJson, @SedeId, 1, GETDATE());
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        Codigo = codigo,
                        dto.SeccionCodigo,
                        Fecha = string.IsNullOrWhiteSpace(dto.Fecha)
                            ? DateTime.Today.ToString("yyyy-MM-dd")
                            : dto.Fecha,
                        DatosJson = datosJson,
                        dto.SedeId,
                    });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar registro", error = ex.Message });
            }
        }

        // ─── POST /bitacoras/{id}/generar ─────────────────────────────
        [HttpPost("{id:int}/generar")]
        public async Task<IActionResult> GenerarPdf(int id, [FromBody] GenerarPdfRequest? req)
        {
            try
            {
                // 1. Leer registro
                var registro = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT r.id, r.codigo_bitacora AS codigoBitacora, r.seccion_codigo AS seccionCodigo,
                             r.fecha, r.sede_id AS sedeId,
                             d.nombre AS nombreBitacora,
                             s.nombre AS nombreSeccion
                       FROM dbo.bitacoras_registros r
                       JOIN dbo.bitacoras_definicion d ON d.codigo = r.codigo_bitacora
                       JOIN dbo.bitacoras_secciones  s ON s.codigo = r.seccion_codigo
                      WHERE r.id = @Id AND r.activo = 1",
                    new { Id = id });

                if (registro == null)
                    return NotFound(new { message = "Registro no encontrado" });

                // 2. Leer columnas y datos
                var columnas = await _db.QueryAsync(
                    @"SELECT campo, label, orden FROM dbo.bitacoras_columnas
                      WHERE codigo_bitacora = @Codigo AND visible = 1
                      ORDER BY orden",
                    new { Codigo = (string)registro.codigoBitacora });

                var datosJson = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT datos_json FROM dbo.bitacoras_registros WHERE id = @Id",
                    new { Id = id });

                var datosDict = string.IsNullOrEmpty(datosJson)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(datosJson) ?? new();

                var encabezados = columnas.Select(c => (string)c.label).ToList();
                var filas = new List<Dictionary<string, string>> { datosDict };

                // 3. Leer config de firmas para armar los slots visuales del PDF
                var cfg = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT rol_operativo, firmas_operativo,
                             rol_supervisor, firmas_supervisor,
                             rol_gerente, firmas_gerente,
                             firma_recepcion
                      FROM dbo.bitacoras_config WHERE codigo_bitacora = @Codigo",
                    new { Codigo = (string)registro.codigoBitacora });

                var slotsPdf = new List<(string Rol, string Nombre)>();
                if (cfg != null)
                {
                    if ((bool)cfg.rol_operativo)
                        for (int i = 0; i < (int)cfg.firmas_operativo; i++)
                            slotsPdf.Add(("operativo", "Operativo"));
                    if ((bool)cfg.rol_supervisor)
                        for (int i = 0; i < (int)cfg.firmas_supervisor; i++)
                            slotsPdf.Add(("supervisor", "Supervisor"));
                    if ((bool)cfg.rol_gerente)
                        for (int i = 0; i < (int)cfg.firmas_gerente; i++)
                            slotsPdf.Add(("gerente", "Gerente"));
                    if ((bool)cfg.firma_recepcion)
                        slotsPdf.Add(("recepcion", "Recepción"));
                }

                // 4. Generar PDF
                var generadoPor = req?.GeneradoPor ?? "";
                var pdfData = new BitacoraPdfData
                {
                    RegistroId     = id,
                    SedeId         = (int)registro.sedeId,
                    CodigoBitacora = (string)registro.codigoBitacora,
                    NombreBitacora = (string)registro.nombreBitacora,
                    Seccion        = (string)registro.nombreSeccion,
                    Sede           = req?.NombreSede ?? "",
                    Fecha          = registro.fecha?.ToString("dd/MM/yyyy") ?? "",
                    GeneradoPor    = generadoPor,
                    Encabezados    = encabezados,
                    Filas          = filas,
                    SlotsFirma     = slotsPdf,
                };

                var pdfRelPath = _pdf.Generar(pdfData);
                var baseUrl = _config["App:BaseUrl"] ?? "";
                var pdfUrl = $"{baseUrl}/api/bitacoras/pdf/{pdfRelPath}";

                // 5. Guardar pdf_path en el registro
                await _db.ExecuteAsync(
                    @"UPDATE dbo.bitacoras_registros
                         SET pdf_path        = @PdfPath,
                             pdf_generado_en = GETDATE(),
                             pdf_generado_por = @GeneradoPor
                       WHERE id = @Id",
                    new { PdfPath = pdfRelPath, GeneradoPor = generadoPor, Id = id });

                return Ok(new { pdfUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar PDF", error = ex.Message });
            }
        }

        // ─── POST /bitacoras/{id}/solicitar-firmas ─────────────────────
        [HttpPost("{id:int}/solicitar-firmas")]
        public async Task<IActionResult> SolicitarFirmas(int id, [FromBody] SolicitarFirmasRequest? req)
        {
            try
            {
                var registro = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT r.id, r.codigo_bitacora AS codigoBitacora,
                             r.fecha, r.sede_id AS sedeId,
                             d.nombre AS nombreBitacora,
                             s.nombre AS nombreSeccion
                       FROM dbo.bitacoras_registros r
                       JOIN dbo.bitacoras_definicion d ON d.codigo = r.codigo_bitacora
                       JOIN dbo.bitacoras_secciones  s ON s.codigo = r.seccion_codigo
                      WHERE r.id = @Id AND r.activo = 1",
                    new { Id = id });

                if (registro == null)
                    return NotFound(new { message = "Registro no encontrado" });

                var cfg = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT rol_operativo, firmas_operativo,
                             rol_supervisor, firmas_supervisor,
                             rol_gerente, firmas_gerente,
                             firma_recepcion
                      FROM dbo.bitacoras_config WHERE codigo_bitacora = @Codigo",
                    new { Codigo = (string)registro.codigoBitacora });

                if (cfg == null)
                    return BadRequest(new { message = "Esta bitácora no tiene configuración de firmas. Configúrala en el panel maestro." });

                // Determinar roles y cantidades requeridas
                var roleSlots = new List<(string Rol, int Count)>();
                if ((bool)cfg.rol_operativo && (int)cfg.firmas_operativo > 0)
                    roleSlots.Add(("operativo", (int)cfg.firmas_operativo));
                if ((bool)cfg.rol_supervisor && (int)cfg.firmas_supervisor > 0)
                    roleSlots.Add(("supervisor", (int)cfg.firmas_supervisor));
                if ((bool)cfg.rol_gerente && (int)cfg.firmas_gerente > 0)
                    roleSlots.Add(("gerente", (int)cfg.firmas_gerente));
                if ((bool)cfg.firma_recepcion)
                    roleSlots.Add(("recepcion", 1));

                if (roleSlots.Count == 0)
                    return BadRequest(new { message = "La configuración de esta bitácora no requiere ninguna firma." });

                // Eliminar slots pendientes previos (re-envío)
                await _db.ExecuteAsync(
                    "DELETE FROM dbo.bitacoras_firmas WHERE registro_id = @Id AND token_usado = 0",
                    new { Id = id });

                var baseUrl = _config["App:BaseUrl"] ?? "";
                var generadoPor = req?.GeneradoPor ?? "";
                var firmasCreadas = new List<object>();
                var firmasRequeridas = 0;

                foreach (var (rol, count) in roleSlots)
                {
                    var users = await _db.QueryAsync(
                        @"SELECT TOP (@Count) u.id, u.nombre_completo AS nombre,
                                 ISNULL(NULLIF(u.username,''), u.nombre_completo) AS email
                          FROM dbo.usuarios u
                          JOIN dbo.roles r ON r.id = u.rol_id
                          WHERE LOWER(r.nombre_rol) LIKE '%' + LOWER(@Rol) + '%'
                            AND u.sede_id = @SedeId
                            AND u.activo = 1
                          ORDER BY u.id",
                        new { Count = count, Rol = rol, SedeId = (int)registro.sedeId });

                    foreach (var user in users)
                    {
                        firmasRequeridas++;
                        var token = Guid.NewGuid().ToString("N");
                        var slotId = await _db.QuerySingleAsync<int>(
                            @"INSERT INTO dbo.bitacoras_firmas
                                (registro_id, rol_requerido, usuario_id, nombre_firmante,
                                 token_firma, token_expira, token_usado, created_at)
                              VALUES
                                (@RegistroId, @Rol, @UsuarioId, @Nombre,
                                 @Token, DATEADD(HOUR, 48, GETDATE()), 0, GETDATE());
                              SELECT CAST(SCOPE_IDENTITY() AS INT);",
                            new { RegistroId = id, Rol = rol, UsuarioId = (long)user.id, Nombre = (string)user.nombre, Token = token });

                        firmasCreadas.Add(new
                        {
                            id             = slotId,
                            rolRequerido   = rol,
                            nombreFirmante = (string)user.nombre,
                            tokenUsado     = false,
                            firmadoEn      = (object?)null,
                        });

                        var emailUser = (string)user.email;
                        if (!string.IsNullOrEmpty(emailUser) && emailUser.Contains('@'))
                        {
                            var urlFirma = $"{baseUrl}/firmar-bitacora?token={token}";
                            var html = BuildEmailHtml(
                                (string)user.nombre,
                                (string)registro.nombreBitacora,
                                registro.fecha?.ToString("dd/MM/yyyy") ?? "",
                                (string)registro.nombreSeccion,
                                urlFirma,
                                generadoPor);
                            try
                            {
                                await _email.SendAsync(emailUser, (string)user.nombre,
                                    $"Solicitud de firma: {(string)registro.nombreBitacora}", html);
                            }
                            catch { /* continuar si el email falla */ }
                        }
                    }
                }

                await _db.ExecuteAsync(
                    @"UPDATE dbo.bitacoras_registros
                         SET firmas_requeridas  = @FirmasRequeridas,
                             firmas_completadas = 0
                       WHERE id = @Id",
                    new { FirmasRequeridas = firmasRequeridas, Id = id });

                return Ok(new
                {
                    firmas = firmasCreadas,
                    firmasRequeridas,
                    message = $"Se enviaron correos a {firmasCreadas.Count} firmante(s).",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al solicitar firmas", error = ex.Message });
            }
        }

        // ─── GET /bitacoras/{id}/firmas ───────────────────────────────
        [HttpGet("{id:int}/firmas")]
        public async Task<IActionResult> GetFirmas(int id)
        {
            var firmas = await _db.QueryAsync(
                @"SELECT
                    id,
                    rol_requerido  AS rolRequerido,
                    nombre_firmante AS nombreFirmante,
                    usuario_id     AS usuarioId,
                    token_usado    AS tokenUsado,
                    firmado_en     AS firmadoEn,
                    ip_firma       AS ipFirma
                  FROM dbo.bitacoras_firmas
                  WHERE registro_id = @Id
                  ORDER BY id",
                new { Id = id });
            return Ok(firmas);
        }

        // ─── GET /bitacoras/pdf/{sedeId}/{filename} — Servir PDF ─────
        [HttpGet("pdf/{sedeId:int}/{filename}")]
        public IActionResult ServirPdf(int sedeId, string filename)
        {
            // Sanitizar: no permitir traversal
            if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
                return BadRequest();

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var filePath = Path.Combine(env.ContentRootPath, "bitacoras-pdfs",
                sedeId.ToString(), filename);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            return PhysicalFile(filePath, "application/pdf");
        }

        // ─── PUT /bitacoras/{id}/status ───────────────────────────────
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> ActualizarStatus(int id, [FromBody] StatusRequest dto)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.bitacoras_registros SET status = @Status WHERE id = @Id AND activo = 1",
                new { dto.Status, Id = id });
            return Ok();
        }

        // ─── DELETE /bitacoras/{id} ───────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.bitacoras_registros SET activo = 0 WHERE id = @Id",
                new { Id = id });
            return Ok();
        }

        // ─── GET /bitacoras/mis-notificaciones?sedeId=X ──────────────
        [Authorize]
        [HttpGet("mis-notificaciones")]
        public async Task<IActionResult> GetMisNotificaciones([FromQuery] int sedeId)
        {
            var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authId)) return Unauthorized();

            var usuario = await _db.QueryFirstOrDefaultAsync(
                @"SELECT u.id, LOWER(r.nombre_rol) AS rol
                  FROM dbo.usuarios u
                  JOIN dbo.roles r ON r.id = u.rol_id
                  WHERE (CAST(u.id AS NVARCHAR(50)) = @AuthId OR CAST(u.auth_user_id AS NVARCHAR(50)) = @AuthId)
                    AND u.activo = 1",
                new { AuthId = authId });

            if (usuario == null) return Unauthorized();

            var userRol = ((string?)usuario.rol ?? "").ToLower();

            var notificaciones = await _db.QueryAsync(
                @"SELECT
                    df.id              AS firmaId,
                    df.documento_id    AS documentoId,
                    df.rol_requerido   AS rolRequerido,
                    df.etiqueta,
                    d.codigo_bitacora  AS codigoBitacora,
                    d.sede_id          AS sedeId,
                    d.fecha,
                    d.status,
                    bd.nombre          AS nombreBitacora,
                    bd.seccion_codigo  AS seccionCodigo,
                    bs.nombre          AS nombreSeccion,
                    bs.color           AS seccionColor,
                    bs.icono           AS seccionIcono
                  FROM dbo.bitacoras_documento_firmas df
                  JOIN dbo.bitacoras_documentos d   ON d.id  = df.documento_id
                  JOIN dbo.bitacoras_definicion bd  ON bd.codigo = d.codigo_bitacora
                  JOIN dbo.bitacoras_secciones  bs  ON bs.codigo = bd.seccion_codigo
                  WHERE df.usuario_id  IS NULL
                    AND df.firma_texto IS NULL
                    AND d.status != 'Firmado'
                    AND (@SedeId = 0 OR d.sede_id = @SedeId)
                    AND (LOWER(@UserRol) LIKE LOWER(df.rol_requerido) + '%'
                         OR (LOWER(@UserRol) IN ('admin','administrador') AND LOWER(df.rol_requerido) = 'gerente'))",
                new { SedeId = sedeId, UserRol = userRol });

            return Ok(notificaciones);
        }

        // ─── GET /bitacoras/documentos?codigoBitacora=X&sedeId=Y ──────
        [HttpGet("documentos")]
        public async Task<IActionResult> GetDocumento([FromQuery] string codigoBitacora, [FromQuery] int sedeId)
        {
            var cfgPer = await _db.QueryFirstOrDefaultAsync(
                "SELECT periodicidad FROM dbo.bitacoras_config WHERE codigo_bitacora = @Codigo",
                new { Codigo = codigoBitacora });
            var periodoInicio = GetPeriodStart((string?)cfgPer?.periodicidad);

            var doc = await _db.QueryFirstOrDefaultAsync(
                @"SELECT TOP 1 id, codigo_bitacora AS codigoBitacora, sede_id AS sedeId, fecha, status
                  FROM dbo.bitacoras_documentos
                  WHERE codigo_bitacora = @Codigo AND sede_id = @SedeId
                    AND fecha >= @PeriodoInicio
                  ORDER BY fecha DESC",
                new { Codigo = codigoBitacora, SedeId = sedeId, PeriodoInicio = periodoInicio });

            if (doc == null) return Ok(null);

            var firmas = await _db.QueryAsync(
                @"SELECT id, rol_requerido AS rolRequerido, etiqueta, orden,
                         usuario_id AS usuarioId, nombre_firmante AS nombreFirmante,
                         firma_texto AS firmaTexto, firmado_en AS firmadoEn
                  FROM dbo.bitacoras_documento_firmas
                  WHERE documento_id = @DocId
                  ORDER BY orden, id",
                new { DocId = (int)doc.id });

            return Ok(new { documento = doc, firmas });
        }

        // ─── POST /bitacoras/documentos/generar ───────────────────────
        [HttpPost("documentos/generar")]
        public async Task<IActionResult> GenerarDocumento([FromBody] GenerarDocumentoRequest dto)
        {
            try
            {
                var cfg = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT periodicidad,
                             rol_operativo, firmas_operativo, etiqueta_operativo,
                             rol_supervisor, firmas_supervisor, etiqueta_supervisor,
                             rol_gerente, firmas_gerente, etiqueta_gerente,
                             firma_recepcion, etiqueta_recepcion
                      FROM dbo.bitacoras_config WHERE codigo_bitacora = @Codigo",
                    new { Codigo = dto.CodigoBitacora });

                var def = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT tipo, fuente_query AS fuenteQuery
                      FROM dbo.bitacoras_definicion WHERE codigo = @Codigo AND activo = 1",
                    new { Codigo = dto.CodigoBitacora });

                if (def == null)
                    return NotFound(new { message = "Bitácora no encontrada" });

                var fecha = dto.Fecha ?? DateTime.Today.ToString("yyyy-MM-dd");

                // Determinar inicio del período para respetar periodicidad
                var periodicidad = cfg != null ? (string?)cfg.periodicidad : null;
                var periodoInicio = GetPeriodStart(periodicidad);

                // Find-or-create: buscar documento existente dentro del período actual
                int documentoId;
                var existente = await _db.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 id FROM dbo.bitacoras_documentos
                      WHERE codigo_bitacora = @Codigo AND sede_id = @SedeId
                        AND fecha >= @PeriodoInicio
                      ORDER BY fecha DESC",
                    new { Codigo = dto.CodigoBitacora, SedeId = dto.SedeId, PeriodoInicio = periodoInicio });

                if (existente.HasValue && existente.Value > 0)
                {
                    documentoId = existente.Value;
                }
                else
                {
                    documentoId = await _db.QuerySingleAsync<int>(
                        @"INSERT INTO dbo.bitacoras_documentos (codigo_bitacora, sede_id, fecha, status, created_at)
                          VALUES (@Codigo, @SedeId, @Fecha, 'Pendiente', GETDATE());
                          SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new { Codigo = dto.CodigoBitacora, SedeId = dto.SedeId, Fecha = fecha });
                }

                // Crear slots de firma si no existen
                if (cfg != null)
                {
                    var slots = new List<(string Rol, string Etiqueta, int Orden)>();
                    if ((bool)cfg.rol_operativo)
                        slots.Add(("operativo", (string?)cfg.etiqueta_operativo ?? "Operativo", 1));
                    if ((bool)cfg.rol_supervisor)
                        slots.Add(("supervisor", (string?)cfg.etiqueta_supervisor ?? "Supervisor", 2));
                    if ((bool)cfg.rol_gerente)
                        slots.Add(("gerente", (string?)cfg.etiqueta_gerente ?? "Gerente", 3));
                    if ((bool)cfg.firma_recepcion)
                        slots.Add(("recepcion", (string?)cfg.etiqueta_recepcion ?? "Recepción", 4));

                    foreach (var (rol, etiqueta, orden) in slots)
                    {
                        await _db.ExecuteAsync(
                            @"IF NOT EXISTS (SELECT 1 FROM dbo.bitacoras_documento_firmas
                                            WHERE documento_id = @DocId AND rol_requerido = @Rol)
                              INSERT INTO dbo.bitacoras_documento_firmas
                                (documento_id, rol_requerido, etiqueta, orden)
                              VALUES (@DocId, @Rol, @Etiqueta, @Orden)",
                            new { DocId = documentoId, Rol = rol, Etiqueta = etiqueta, Orden = orden });
                    }

                    // Recalcular status según firmas reales (por si se agregaron nuevos slots)
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.bitacoras_documentos
                          SET status = CASE
                            WHEN (SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                                  WHERE documento_id = @Id AND usuario_id IS NULL) = 0
                             AND (SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                                  WHERE documento_id = @Id) > 0
                            THEN 'Firmado'
                            WHEN (SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                                  WHERE documento_id = @Id AND usuario_id IS NOT NULL) > 0
                            THEN 'En proceso'
                            ELSE 'Pendiente'
                          END
                          WHERE id = @Id",
                        new { Id = documentoId });
                }

                // Obtener filas del período
                string tipo = (string)def.tipo;
                string? fuenteQuery = (string?)def.fuenteQuery;
                var filas = new List<Dictionary<string, object?>>();

                if (tipo == "linked" && !string.IsNullOrWhiteSpace(fuenteQuery))
                {
                    if (!fuenteQuery.StartsWith("vw_bitacora_", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { message = "Nombre de vista inválido" });

                    var rows = await _db.QueryAsync(
                        $@"SELECT * FROM dbo.{fuenteQuery}
                           WHERE (@SedeId = 0 OR sede_id = @SedeId)
                             AND CAST(fecha AS DATE) >= @PeriodoInicio
                             AND CAST(fecha AS DATE) <= CAST(GETDATE() AS DATE)
                           ORDER BY fecha, id",
                        new { SedeId = dto.SedeId, PeriodoInicio = periodoInicio });

                    filas = rows.Select(r =>
                    {
                        var dict = (IDictionary<string, object?>)r;
                        return dict.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value);
                    }).ToList();
                }
                else
                {
                    var rows = await _db.QueryAsync(
                        @"SELECT datos_json FROM dbo.bitacoras_registros
                          WHERE codigo_bitacora = @Codigo AND sede_id = @SedeId
                            AND CAST(fecha AS DATE) >= @PeriodoInicio
                            AND CAST(fecha AS DATE) <= CAST(GETDATE() AS DATE)
                            AND activo = 1
                          ORDER BY fecha, id",
                        new { Codigo = dto.CodigoBitacora, SedeId = dto.SedeId, PeriodoInicio = periodoInicio });

                    foreach (var r in rows)
                        if (!string.IsNullOrEmpty((string?)r.datos_json))
                        {
                            var d = JsonSerializer.Deserialize<Dictionary<string, object?>>(r.datos_json);
                            if (d != null) filas.Add(d);
                        }
                }

                var columnas = await _db.QueryAsync(
                    @"SELECT campo, label, tipo_dato AS tipoDato, es_meta AS esMeta, orden
                      FROM dbo.bitacoras_columnas
                      WHERE codigo_bitacora = @Codigo AND visible = 1
                      ORDER BY orden",
                    new { Codigo = dto.CodigoBitacora });

                var firmas = await _db.QueryAsync(
                    @"SELECT id, rol_requerido AS rolRequerido, etiqueta, orden,
                             usuario_id AS usuarioId, nombre_firmante AS nombreFirmante,
                             firma_texto AS firmaTexto, firmado_en AS firmadoEn
                      FROM dbo.bitacoras_documento_firmas
                      WHERE documento_id = @DocId
                      ORDER BY orden, id",
                    new { DocId = documentoId });

                var docStatus = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT status FROM dbo.bitacoras_documentos WHERE id = @Id",
                    new { Id = documentoId });

                return Ok(new { documentoId, status = docStatus, columnas, filas, firmas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar documento", error = ex.Message });
            }
        }

        // ─── DELETE /bitacoras/documentos/{documentoId} ───────────────
        [HttpDelete("documentos/{documentoId:int}")]
        public async Task<IActionResult> EliminarDocumento(int documentoId)
        {
            // Solo se puede eliminar si ninguna firma ha sido completada
            var firmadas = await _db.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                  WHERE documento_id = @Id AND firma_texto IS NOT NULL",
                new { Id = documentoId });

            if (firmadas > 0)
                return BadRequest(new { message = "No se puede reiniciar: el documento ya tiene firmas registradas." });

            await _db.ExecuteAsync(
                "DELETE FROM dbo.bitacoras_documento_firmas WHERE documento_id = @Id",
                new { Id = documentoId });

            await _db.ExecuteAsync(
                "DELETE FROM dbo.bitacoras_documentos WHERE id = @Id",
                new { Id = documentoId });

            return Ok(new { message = "Documento reiniciado" });
        }

        // ─── POST /bitacoras/documentos/{documentoId}/firmar ──────────
        [Authorize]
        [HttpPost("documentos/{documentoId:int}/firmar")]
        public async Task<IActionResult> FirmarDocumento(int documentoId, [FromBody] FirmarDocumentoRequest req)
        {
            try
            {
                var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authId))
                    return Unauthorized(new { message = "Sesión no válida" });

                var usuario = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT u.id, u.nombre_completo AS nombre, u.firma, u.nip_hash, LOWER(r.nombre_rol) AS rol
                      FROM dbo.usuarios u
                      JOIN dbo.roles r ON r.id = u.rol_id
                      WHERE (CAST(u.id AS NVARCHAR(50)) = @AuthId OR CAST(u.auth_user_id AS NVARCHAR(50)) = @AuthId)
                        AND u.activo = 1",
                    new { AuthId = authId });

                if (usuario == null)
                    return Unauthorized(new { message = "Usuario no encontrado" });

                if (string.IsNullOrEmpty((string?)usuario.nip_hash))
                    return BadRequest(new { message = "No tienes NIP configurado. Configúralo en tu perfil de usuario." });

                if (!BCrypt.Net.BCrypt.Verify(req.Nip, (string)usuario.nip_hash))
                    return StatusCode(401, new { message = "NIP incorrecto" });

                var userRol = (string?)usuario.rol ?? "";
                // LIKE match: 'supervisor_bascula' coincide con rol_requerido 'supervisor'
                var slot = await _db.QueryFirstOrDefaultAsync(
                    @"SELECT id, orden FROM dbo.bitacoras_documento_firmas
                      WHERE documento_id = @DocId
                        AND (LOWER(@Rol) LIKE LOWER(rol_requerido) + '%'
                             OR (LOWER(@Rol) IN ('admin','administrador') AND LOWER(rol_requerido) = 'gerente'))
                        AND usuario_id IS NULL",
                    new { DocId = documentoId, Rol = userRol });

                if (slot == null)
                    return BadRequest(new { message = "No hay slot de firma pendiente para tu rol en este documento." });

                // Validar que no haya firmas pendientes con orden menor (respetar secuencia)
                var pendientesAnteriores = await _db.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                      WHERE documento_id = @DocId AND orden < @Orden AND firma_texto IS NULL",
                    new { DocId = documentoId, Orden = (int)slot.orden });

                if (pendientesAnteriores > 0)
                    return BadRequest(new { message = "Debes esperar a que los firmantes anteriores completen su firma primero." });

                if (_db.State == ConnectionState.Closed) _db.Open();
                using var trans = _db.BeginTransaction();
                try
                {
                    await _db.ExecuteAsync(
                        @"UPDATE dbo.bitacoras_documento_firmas
                             SET usuario_id      = @UserId,
                                 nombre_firmante = @Nombre,
                                 firma_texto     = @Firma,
                                 firmado_en      = GETDATE()
                           WHERE id = @SlotId",
                        new { UserId = (long)usuario.id, Nombre = (string)usuario.nombre,
                              Firma = string.IsNullOrWhiteSpace((string?)usuario.firma) ? (string)usuario.nombre : (string?)usuario.firma,
                              SlotId = (int)slot.id },
                        transaction: trans);

                    var pendientes = await _db.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(*) FROM dbo.bitacoras_documento_firmas
                          WHERE documento_id = @DocId AND usuario_id IS NULL",
                        new { DocId = documentoId }, transaction: trans);

                    if (pendientes == 0)
                        await _db.ExecuteAsync(
                            "UPDATE dbo.bitacoras_documentos SET status = 'Firmado' WHERE id = @Id",
                            new { Id = documentoId }, transaction: trans);

                    trans.Commit();
                }
                catch { trans.Rollback(); throw; }

                var firmas = await _db.QueryAsync(
                    @"SELECT id, rol_requerido AS rolRequerido, etiqueta, orden,
                             usuario_id AS usuarioId, nombre_firmante AS nombreFirmante,
                             firma_texto AS firmaTexto, firmado_en AS firmadoEn
                      FROM dbo.bitacoras_documento_firmas
                      WHERE documento_id = @DocId ORDER BY orden, id",
                    new { DocId = documentoId });

                var nuevoStatus = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT status FROM dbo.bitacoras_documentos WHERE id = @Id",
                    new { Id = documentoId });

                return Ok(new { firmas, status = nuevoStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al registrar firma", error = ex.Message });
            }
        }

        // ─── Helper: inicio del período actual ────────────────────────
        private static DateTime GetPeriodStart(string? periodicidad)
        {
            var hoy = DateTime.Today;
            return (periodicidad ?? "Diaria").ToLower() switch
            {
                "semanal" => hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)), // lunes
                "mensual" => new DateTime(hoy.Year, hoy.Month, 1),
                "anual"   => new DateTime(hoy.Year, 1, 1),
                _         => hoy, // diaria
            };
        }

        // ─── Helpers ──────────────────────────────────────────────────
        private static string BuildEmailHtml(string nombre, string bitacora, string fecha,
            string seccion, string urlFirma, string generadoPor)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#f5f5f5;margin:0;padding:20px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1)'>
    <div style='background:#1a2233;padding:24px;text-align:center'>
      <h2 style='color:#fff;margin:0;font-size:20px'>Sistema Alazan</h2>
      <p style='color:#aaa;margin:6px 0 0;font-size:13px'>Solicitud de Firma Digital</p>
    </div>
    <div style='padding:28px'>
      <p style='color:#333;font-size:15px'>Hola, <strong>{nombre}</strong></p>
      <p style='color:#555;font-size:14px'>Se te solicita firmar digitalmente la siguiente bitácora:</p>
      <div style='background:#f9f9f9;border-left:4px solid #1a2233;padding:14px 18px;border-radius:0 6px 6px 0;margin:16px 0'>
        <p style='margin:0 0 6px;font-weight:bold;color:#1a2233;font-size:15px'>{bitacora}</p>
        <p style='margin:0;color:#666;font-size:13px'>Sección: {seccion}  |  Fecha: {fecha}</p>
        <p style='margin:4px 0 0;color:#888;font-size:12px'>Solicitado por: {generadoPor}</p>
      </div>
      <p style='color:#555;font-size:13px'>Para firmar, haz clic en el botón y luego ingresa tu NIP personal:</p>
      <div style='text-align:center;margin:24px 0'>
        <a href='{urlFirma}'
           style='background:#1a2233;color:#fff;padding:12px 32px;border-radius:6px;
                  text-decoration:none;font-size:15px;font-weight:bold;display:inline-block'>
          Firmar Bitácora
        </a>
      </div>
      <p style='color:#aaa;font-size:11px;text-align:center;margin-top:16px'>
        Este enlace expira en 48 horas. Si no reconoces esta solicitud, ignora este mensaje.
      </p>
    </div>
  </div>
</body>
</html>";
        }
    }

    // ─── DTOs ─────────────────────────────────────────────────────────
    public class BitacoraRegistroRequest
    {
        public string Fecha         { get; set; } = "";
        public string SeccionCodigo { get; set; } = "";
        public int    SedeId        { get; set; }
        public Dictionary<string, string> Datos { get; set; } = new();
    }

    public class StatusRequest
    {
        public string Status { get; set; } = "Pendiente";
    }

    public class GenerarPdfRequest
    {
        public string? GeneradoPor { get; set; }
        public string? NombreSede  { get; set; }
    }

    public class SolicitarFirmasRequest
    {
        public string? GeneradoPor { get; set; }
    }

    public class FirmarRequest
    {
        public string Token { get; set; } = "";
        public string Nip   { get; set; } = "";
    }

    public class GenerarDocumentoRequest
    {
        public string  CodigoBitacora { get; set; } = "";
        public int     SedeId         { get; set; }
        public string? Fecha          { get; set; }
    }

    public class FirmarDocumentoRequest
    {
        public string Nip { get; set; } = "";
    }
}
