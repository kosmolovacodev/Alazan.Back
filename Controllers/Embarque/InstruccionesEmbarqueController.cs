using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    [Route("instrucciones-embarque")]
    public class InstruccionesEmbarqueController : ControllerBase
    {
        private readonly IDbConnection _db;
        public InstruccionesEmbarqueController(IDbConnection db) => _db = db;

        // ─── GET /instrucciones-embarque/historial?sedeId=1 ───────────
        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial([FromQuery] int sedeId)
        {
            var rows = await _db.QueryAsync(
                @"SELECT
                    ie.id,
                    ie.no_instruccion        AS noInstruccion,
                    ie.referencia_alazan     AS referenciaAlazan,
                    CONVERT(DATE, ie.fecha)  AS fecha,
                    ie.cliente,
                    ie.domicilio,
                    ie.contrato,
                    ie.broker,
                    ie.producto,
                    ie.calibre,
                    ie.tons,
                    ie.precio_unitario       AS precioUnitario,
                    ie.fecha_embarque        AS fechaEmbarque,
                    ie.lugar_embarque        AS lugarEmbarque,
                    ie.status_embarque       AS statusEmbarque,
                    ie.status_documentacion  AS statusDocumentacion,
                    ie.condiciones_generales  AS condicionesGenerales,
                    ie.condiciones_especiales AS condicionesEspeciales,
                    p.nombre                 AS presentacion,
                    ie.presentacion_id,
                    ie.plantilla_id,
                    pl.titulo                AS plantillaTitulo,
                    ie.sede_id,
                    ie.created_at            AS creadoEn
                  FROM dbo.instrucciones_embarque ie
                  LEFT JOIN dbo.cat_ie_presentacion p  ON p.id  = ie.presentacion_id
                  LEFT JOIN dbo.cat_ie_plantilla     pl ON pl.id = ie.plantilla_id
                  WHERE ie.activo = 1
                    AND (@SedeId = 0 OR ie.sede_id = @SedeId)
                  ORDER BY ie.fecha DESC, ie.id DESC",
                new { SedeId = sedeId });
            return Ok(rows);
        }

        // ─── GET /instrucciones-embarque/stats?sedeId=1 ───────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int sedeId)
        {
            var data = await _db.QueryFirstOrDefaultAsync(
                @"SELECT
                    COUNT(*)                                                        AS total,
                    ISNULL(SUM(tons), 0)                                            AS totalTons,
                    COUNT(CASE WHEN status_embarque = 'Pendiente'   THEN 1 END)     AS pendiente,
                    ISNULL(SUM(CASE WHEN status_embarque = 'Pendiente'   THEN tons END), 0) AS tonsPendiente,
                    COUNT(CASE WHEN status_embarque = 'En Tránsito' THEN 1 END)     AS enTransito,
                    ISNULL(SUM(CASE WHEN status_embarque = 'En Tránsito' THEN tons END), 0) AS tonsTransito,
                    COUNT(CASE WHEN status_embarque = 'Embarcado'   THEN 1 END)     AS embarcado,
                    ISNULL(SUM(CASE WHEN status_embarque = 'Embarcado'   THEN tons END), 0) AS tonsEmbarcado,
                    COUNT(CASE WHEN status_embarque = 'Cancelado'   THEN 1 END)     AS cancelado,
                    ISNULL(SUM(CASE WHEN status_embarque = 'Cancelado'   THEN tons END), 0) AS tonsCancelado,
                    COUNT(CASE WHEN producto = 'Garbanzo' THEN 1 END)               AS garbanzo,
                    ISNULL(SUM(CASE WHEN producto = 'Garbanzo' THEN tons END), 0)   AS tonsGarbanzo,
                    COUNT(CASE WHEN producto = 'Frijol'   THEN 1 END)               AS frijol,
                    ISNULL(SUM(CASE WHEN producto = 'Frijol'   THEN tons END), 0)   AS tonsFrijol
                  FROM dbo.instrucciones_embarque
                  WHERE activo = 1
                    AND (@SedeId = 0 OR sede_id = @SedeId)",
                new { SedeId = sedeId });
            return Ok(data);
        }

        // ─── GET /instrucciones-embarque/{id} ─────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var row = await _db.QueryFirstOrDefaultAsync(
                @"SELECT
                    ie.id,
                    ie.no_instruccion        AS noInstruccion,
                    ie.referencia_alazan     AS referenciaAlazan,
                    CONVERT(DATE, ie.fecha)  AS fecha,
                    ie.cliente,
                    ie.domicilio,
                    ie.contrato,
                    ie.broker,
                    ie.producto,
                    ie.calibre,
                    ie.tons,
                    ie.precio_unitario       AS precioUnitario,
                    ie.fecha_embarque        AS fechaEmbarque,
                    ie.lugar_embarque        AS lugarEmbarque,
                    ie.status_embarque       AS statusEmbarque,
                    ie.status_documentacion  AS statusDocumentacion,
                    ie.condiciones_generales  AS condicionesGenerales,
                    ie.condiciones_especiales AS condicionesEspeciales,
                    p.nombre                 AS presentacion,
                    ie.presentacion_id,
                    ie.plantilla_id,
                    pl.titulo                AS plantillaTitulo,
                    pl.cuerpo                AS plantillaCuerpo,
                    ie.sede_id
                  FROM dbo.instrucciones_embarque ie
                  LEFT JOIN dbo.cat_ie_presentacion p  ON p.id  = ie.presentacion_id
                  LEFT JOIN dbo.cat_ie_plantilla     pl ON pl.id = ie.plantilla_id
                  WHERE ie.id = @Id AND ie.activo = 1",
                new { Id = id });

            if (row == null) return NotFound();
            return Ok(row);
        }

        // ─── GET /instrucciones-embarque/catalogos ────────────────────
        [HttpGet("catalogos")]
        public async Task<IActionResult> GetCatalogos()
        {
            var presentaciones = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_ie_presentacion WHERE activo = 1 ORDER BY nombre");
            var brokers = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_ie_broker WHERE activo = 1 ORDER BY nombre");
            var lugares = await _db.QueryAsync(
                "SELECT id, nombre FROM dbo.cat_ie_lugar WHERE activo = 1 ORDER BY nombre");
            var plantillas = await _db.QueryAsync(
                "SELECT id, titulo, cuerpo FROM dbo.cat_ie_plantilla WHERE activo = 1 ORDER BY titulo");

            return Ok(new { presentaciones, brokers, lugares, plantillas });
        }

        // ─── POST /instrucciones-embarque ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] InstruccionRequest dto)
        {
            try
            {
                var id = await _db.QuerySingleAsync<int>(
                    @"INSERT INTO dbo.instrucciones_embarque
                        (no_instruccion, referencia_alazan, fecha, cliente, domicilio, contrato,
                         broker, producto, calibre, tons, precio_unitario,
                         presentacion_id, fecha_embarque, lugar_embarque,
                         status_embarque, status_documentacion,
                         condiciones_generales, condiciones_especiales,
                         plantilla_id, sede_id, activo, created_at)
                      VALUES
                        (@NoInstruccion, @ReferenciaAlazan, @Fecha, @Cliente, @Domicilio, @Contrato,
                         @Broker, @Producto, @Calibre, @Tons, @PrecioUnitario,
                         @PresentacionId, @FechaEmbarque, @LugarEmbarque,
                         @StatusEmbarque, @StatusDocumentacion,
                         @CondicionesGenerales, @CondicionesEspeciales,
                         @PlantillaId, @SedeId, 1, GETDATE());
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    dto);
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear instrucción", error = ex.Message });
            }
        }

        // ─── PUT /instrucciones-embarque/{id} ─────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] InstruccionRequest dto)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"UPDATE dbo.instrucciones_embarque SET
                        no_instruccion        = @NoInstruccion,
                        referencia_alazan     = @ReferenciaAlazan,
                        fecha                 = @Fecha,
                        cliente               = @Cliente,
                        domicilio             = @Domicilio,
                        contrato              = @Contrato,
                        broker                = @Broker,
                        producto              = @Producto,
                        calibre               = @Calibre,
                        tons                  = @Tons,
                        precio_unitario       = @PrecioUnitario,
                        presentacion_id       = @PresentacionId,
                        fecha_embarque        = @FechaEmbarque,
                        lugar_embarque        = @LugarEmbarque,
                        status_embarque       = @StatusEmbarque,
                        status_documentacion  = @StatusDocumentacion,
                        condiciones_generales  = @CondicionesGenerales,
                        condiciones_especiales = @CondicionesEspeciales,
                        plantilla_id          = @PlantillaId,
                        updated_at            = GETDATE()
                      WHERE id = @Id AND activo = 1",
                    new { dto.NoInstruccion, dto.ReferenciaAlazan, dto.Fecha,
                          dto.Cliente, dto.Domicilio, dto.Contrato, dto.Broker,
                          dto.Producto, dto.Calibre, dto.Tons, dto.PrecioUnitario,
                          dto.PresentacionId, dto.FechaEmbarque, dto.LugarEmbarque,
                          dto.StatusEmbarque, dto.StatusDocumentacion,
                          dto.CondicionesGenerales, dto.CondicionesEspeciales,
                          dto.PlantillaId, Id = id });
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
            }
        }

        // ─── DELETE /instrucciones-embarque/{id} ──────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _db.ExecuteAsync(
                "UPDATE dbo.instrucciones_embarque SET activo = 0 WHERE id = @Id",
                new { Id = id });
            return Ok();
        }
    }

    public class InstruccionRequest
    {
        public string   NoInstruccion        { get; set; } = "";
        public string?  ReferenciaAlazan     { get; set; }
        public string   Fecha                { get; set; } = "";
        public string?  Cliente              { get; set; }
        public string?  Domicilio            { get; set; }
        public string?  Contrato             { get; set; }
        public string?  Broker               { get; set; }
        public string   Producto             { get; set; } = "Garbanzo";
        public string?  Calibre              { get; set; }
        public decimal  Tons                 { get; set; }
        public string?  PrecioUnitario       { get; set; }
        public int?     PresentacionId       { get; set; }
        public string?  FechaEmbarque        { get; set; }
        public string?  LugarEmbarque        { get; set; }
        public string   StatusEmbarque       { get; set; } = "Pendiente";
        public string   StatusDocumentacion  { get; set; } = "Incompleto";
        public string?  CondicionesGenerales  { get; set; }
        public string?  CondicionesEspeciales { get; set; }
        public int?     PlantillaId          { get; set; }
        public int      SedeId               { get; set; }
    }
}
