using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Data; // Para IDbConnection
using Microsoft.Data.SqlClient; // Para SqlConnection
using Dapper; // Para los métodos ExecuteAsync o QueryAsync
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

[ApiController]
[Route("v1")]
public class IntegracionMba3Controller : ControllerBase
{
    private readonly IConfiguration _config;

    public IntegracionMba3Controller(IConfiguration config)
    {
        _config = config;
    }

    // 1. ENDPOINT DE AUTENTICACIÓN
    // Aquí es donde TI Alazán enviará su Usuario y Contraseña
    [HttpPost("auth")]
    public IActionResult Login([FromBody] LoginIntegracionRequest request)
    {
        // Leemos las credenciales que pusimos en el appsettings.json
        var usuarioValido = _config["IntegracionMBA3:Usuario"];
        var passwordValido = _config["IntegracionMBA3:Password"];
        var codigoValido = int.Parse(_config["IntegracionMBA3:Codigo"] ?? "0");

        // Validamos si lo que envían coincide
        if (request.Usuario == usuarioValido && 
            request.Password == passwordValido && 
            request.Codigo == codigoValido)
        {
            // Si es correcto, generamos el Token
            var token = GenerarTokenJWT(request.Usuario);
            return Ok(new { 
                token = token,
                expires = DateTime.UtcNow.AddHours(8) // El token durará 8 horas
            });
        }

        // Si fallan las credenciales
        return Unauthorized(new { message = "Código, Usuario o Contraseña incorrectos para MBA3" });
    }

    private string GetConnectionString() => _config.GetConnectionString("DefaultConnection")!;
    // 2. ENDPOINT PARA RECIBIR LA ORDEN (TRAMA)
    // Este está protegido: solo entra quien tenga el Token
    [HttpPost("opE")]
    [Authorize]
    public async Task<IActionResult> PostOrden([FromBody] TramaMba3DTO trama)
    {
        try 
        {
            using (IDbConnection db = new SqlConnection(GetConnectionString()))
            {
                // 1. Serializamos la lista de detalles a un string JSON
                var detalleJson = JsonSerializer.Serialize(trama.detalle);

                // 2. Definimos el SQL (asegúrate de que al final diga @detalleString)
                string sql = @"INSERT INTO Integracion_MBA3_Ordenes 
                    (operacion, codigo_empresa, codigo_sucursal, numero_orden, fecha_ajuste, 
                    codigo_bodega_materia_prima, codigo_bodega_productos_proceso, codigo_bodega_producto_terminado, 
                    tipo_orden, prioridad_orden, estatus_orden, documento_control, numero_lote, unidad_control, 
                    numero_pedido, fecha_requerida_interna, fecha_requerida_cliente, fecha_programada_inicio, 
                    fecha_estimada_entrega, fecha_real_inicio, hora_requerida_interna, hora_requerida_cliente, 
                    hora_programada_inicio, hora_real_inicio, codigo_cliente_pedido, codigo_producto_terminado, 
                    ruta_produccion, cantidad, cantidad_segunda_unidad, multidimension, lote_ubicacion_pro_terminado, 
                    notas_orden, memo_general, memo_bodega, memo_responsable, memo_avance, detalle) 
                    VALUES 
                    (@operacion, @codigo_empresa, @codigo_sucursal, @numero_orden, @fecha_ajuste, 
                    @codigo_bodega_materia_prima, @codigo_bodega_productos_proceso, @codigo_bodega_producto_terminado, 
                    @tipo_orden, @prioridad_orden, @estatus_orden, @documento_control, @numero_lote, @unidad_control, 
                    @numero_pedido, @fecha_requerida_interna, @fecha_requerida_cliente, @fecha_programada_inicio, 
                    @fecha_estimada_entrega, @fecha_real_inicio, @hora_requerida_interna, @hora_requerida_cliente, 
                    @hora_programada_inicio, @hora_real_inicio, @codigo_cliente_pedido, @codigo_producto_terminado, 
                    @ruta_produccion, @cantidad, @cantidad_segunda_unidad, @multidimension, @lote_ubicacion_pro_terminado, 
                    @notas_orden, @memo_general, @memo_bodega, @memo_responsable, @memo_avance, @detalleString)";

                // 3. Creamos los parámetros dinámicos
                var parametros = new DynamicParameters(trama); // Esto mapea todo lo que hay en trama
                parametros.Add("detalleString", detalleJson); // Esto añade la variable que faltaba

                await db.ExecuteAsync(sql, parametros);
            }
            return Ok(new { message = "Orden Completa guardada con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }


    [HttpPost("reE")]
    [Authorize]
    public async Task<IActionResult> PostRecepcion([FromBody] TramaRecepcionDTO trama)
    {
        try 
        {
            using (IDbConnection db = new SqlConnection(GetConnectionString()))
            {
                var detalleJson = JsonSerializer.Serialize(trama.detalle);

                string sql = @"INSERT INTO Integracion_MBA3_Recepciones 
                    (operacion, codigo_empresa, codigo_sucursal, numero_documento, codigo_proveedor, 
                    fecha_recepcion, sucursal_origen, codigo_bodega, memo, hora_recepcion, 
                    nombre_embarcacion, procedencia, codigo_calificador, recepcion_externa, 
                    chofer, codigo_supervisor, campos_alpha_adicionales, campos_date_adicionales, 
                    campos_real_adicionales, detalle) 
                    VALUES 
                    (@operacion, @codigo_empresa, @codigo_sucursal, @numero_documento, @codigo_proveedor, 
                    @fecha_recepcion, @sucursal_origen, @codigo_bodega, @memo, @hora_recepcion, 
                    @nombre_embarcacion, @procedencia, @codigo_calificador, @recepcion_externa, 
                    @chofer, @codigo_supervisor, @cinco_campos_adicionales_alpha, @cinco_campos_adicionales_date, 
                    @cinco_campos_adicionales_real, @detalleString)";

                var parametros = new DynamicParameters(trama);
                parametros.Add("detalleString", detalleJson);

                await db.ExecuteAsync(sql, parametros);
            }
            return Ok(new { message = "Recepción con campos adicionales guardada." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }


    // FUNCIÓN PRIVADA PARA GENERAR EL JWT
    private string GenerarTokenJWT(string usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, usuario),
            new Claim("TipoUsuario", "IntegracionExterna")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


}