using System.Data;
using Dapper;

namespace Alazan.API.Services
{
    public class AutoAutorizacionPrecioService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoAutorizacionPrecioService> _logger;

        public AutoAutorizacionPrecioService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoAutorizacionPrecioService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de auto-autorización de precios iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarYAutorizar();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en auto-autorización de precios");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task VerificarYAutorizar()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            // Obtener todas las sedes con su configuración de tiempo
            var sedes = await db.QueryAsync<dynamic>(@"
                SELECT sede_id, tiempo_autorizacion_auto
                FROM dbo.configuracion_sistema
                WHERE tiempo_autorizacion_auto > 0");

            foreach (var sede in sedes)
            {
                int sedeId = (int)sede.sede_id;
                int minutos = (int)sede.tiempo_autorizacion_auto;

                // 1. Traemos las boletas vencidas con su relación a boletas_precio
                var boletasVencidas = await db.QueryAsync<dynamic>(@"
                    SELECT 
                        b.id, 
                        b.precio_mxn, 
                        b.bascula_id,
                        bp.id AS boleta_precio_id
                    FROM dbo.boletas b
                    LEFT JOIN dbo.boletas_precio bp ON b.id = bp.boleta_id
                    WHERE b.sede_id = @sedeId
                      AND b.status IN ('Sin Precio', 'Pendiente', 'Pendiente por Autorizar')
                      AND DATEDIFF(MINUTE, b.created_at, SYSDATETIMEOFFSET()) >= @minutos",
                    new { sedeId, minutos });

                foreach (var boleta in boletasVencidas)
                {
                    try
                    {
                        if (db.State == ConnectionState.Closed) db.Open();
                        using var trans = db.BeginTransaction();

                        try
                        {
                            // Extraemos valores dinámicos a variables locales para evitar errores de tipos
                            int bId = (int)boleta.id;
                            int basId = (int)boleta.bascula_id;
                            decimal precio = (decimal)boleta.precio_mxn;
                            int? bpId = boleta.boleta_precio_id != null ? (int)boleta.boleta_precio_id : null;

                            // A. Actualizar dbo.boletas
                            await db.ExecuteAsync(@"
                                UPDATE dbo.boletas
                                SET status = 'Precio Autorizado',
                                    observaciones = ISNULL(observaciones, '') + ' | Auto-Auth (' + CAST(@minutos AS VARCHAR) + ' min)',
                                    updated_at = SYSDATETIMEOFFSET()
                                WHERE id = @bId",
                                new { bId, minutos }, transaction: trans);

                            // B. Actualizar dbo.bascula_recepciones
                            await db.ExecuteAsync(@"
                                UPDATE dbo.bascula_recepciones
                                SET status = 'AUTORIZADO',
                                    updated_at = GETDATE()
                                WHERE id = @basId",
                                new { basId }, transaction: trans);

                            // C. Actualizar dbo.boletas_precio si existe el registro
                            if (bpId.HasValue)
                            {
                                await db.ExecuteAsync(@"
                                    UPDATE dbo.boletas_precio
                                    SET estatus = 'Precio Autorizado',
                                        precio_autorizado = @precio,
                                        precio_final = @precio,
                                        tiempo_autorizacion = SYSDATETIMEOFFSET(),
                                        usuario_autorizacion = 'SISTEMA',
                                        autorizacion_automatica = 1,
                                        observaciones = ISNULL(observaciones, '') + ' | Auto-Auth por tiempo',
                                        fecha_modificacion = SYSDATETIMEOFFSET()
                                    WHERE id = @bpId",
                                    new { precio, bpId = bpId.Value }, transaction: trans);
                            }

                            trans.Commit();

                            // LOG CORREGIDO: Evitamos pasar 'dynamic' directamente al logger
                            _logger.LogInformation(
                                "Boleta {BoletaId} (PrecioId: {BPId}) auto-autorizada por tiempo ({Minutos} min)",
                                bId, bpId ?? 0, minutos);
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            _logger.LogError(ex, "Transacción fallida para boleta {Id}", (int)boleta.id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando boleta {Id}", (int)boleta.id);
                    }
                }
            }
        }
    }
}