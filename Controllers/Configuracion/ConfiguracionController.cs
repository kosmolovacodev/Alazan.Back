using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace Alazan.API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")] // La ruta será: api/configuracion
    [Route("[controller]")] // La ruta será: api/configuracion
    public class ConfiguracionController : ControllerBase
    {
        private readonly IDbConnection _db;
        public ConfiguracionController(IDbConnection db) => _db = db;

        // 1. GET para obtener datos filtrados por sede
        [HttpGet]
        public async Task<IActionResult> GetConfig([FromQuery] int sedeId)
        {
            // Si sedeId es 0 (admin global), obtiene la configuración de la sede activa seleccionada
            var sql = "SELECT * FROM dbo.configuracion_sistema WHERE sede_id = @sedeId";
            var config = await _db.QueryFirstOrDefaultAsync(sql, new { sedeId });

            // Si no existe configuración para esta sede, devolver valores por defecto
            if (config == null)
            {
                return Ok(new {
                    sede_id = sedeId,
                    nombre_empresa = "",
                    rfc = "",
                    direccion = "",
                    telefono = "",
                    correo = "",
                    color_primario = "#1976D2",
                    mensaje_ticket = "",
                    tiempo_autorizacion_auto = 30 // Valor por defecto: 30 minutos
                });
            }
            return Ok(config);
        }

        // 2. POST para actualizar datos por sede
        [HttpPost]
        public async Task<IActionResult> UpdateConfig([FromBody] ConfiguracionDto model, [FromQuery] int sedeId)
        {
            if (_db.State == ConnectionState.Closed) _db.Open();
            try
            {
                // Intentamos actualizar el registro de esta sede
                var sql = @"UPDATE dbo.configuracion_sistema
                            SET nombre_empresa = @nombre_empresa,
                                rfc = @rfc,
                                direccion = @direccion,
                                telefono = @telefono,
                                correo = @correo,
                                color_primario = @color_primario,
                                mensaje_ticket = @mensaje_ticket,
                                tiempo_autorizacion_auto = @tiempo_autorizacion_auto
                            WHERE sede_id = @sedeId";

                int affected = await _db.ExecuteAsync(sql, new {
                    model.nombre_empresa, model.rfc, model.direccion,
                    model.telefono, model.correo, model.color_primario,
                    model.mensaje_ticket, model.tiempo_autorizacion_auto, sedeId
                });

                // Si no existía registro para esta sede, lo insertamos
                if (affected == 0)
                {
                    var sqlInsert = @"INSERT INTO dbo.configuracion_sistema
                        (nombre_empresa, rfc, direccion, telefono, correo, color_primario, mensaje_ticket, tiempo_autorizacion_auto, sede_id)
                        VALUES (@nombre_empresa, @rfc, @direccion, @telefono, @correo, @color_primario, @mensaje_ticket, @tiempo_autorizacion_auto, @sedeId)";
                    await _db.ExecuteAsync(sqlInsert, new {
                        model.nombre_empresa, model.rfc, model.direccion,
                        model.telefono, model.correo, model.color_primario,
                        model.mensaje_ticket, model.tiempo_autorizacion_auto, sedeId
                    });
                }

                return Ok(new { message = "Configuración guardada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
            finally
            {
                _db.Close();
            }
        }
    }

    // DTO para configuración
    public class ConfiguracionDto
    {
        public string nombre_empresa { get; set; }
        public string rfc { get; set; }
        public string direccion { get; set; }
        public string telefono { get; set; }
        public string correo { get; set; }
        public string color_primario { get; set; }
        public string mensaje_ticket { get; set; }
        public int tiempo_autorizacion_auto { get; set; } = 30; // Minutos para autorización automática de precio
    }
}