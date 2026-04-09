using System.Data;
using Dapper;

namespace Alazan.API.Services
{
    /// <summary>
    /// Tarea programada que recalcula el snapshot de inventario por silo cada noche.
    /// Llama a dbo.sp_calcular_snapshot_inventario para todas las sedes.
    /// Hora de ejecución: 00:05 (5 minutos después de medianoche).
    /// </summary>
    public class SnapshotInventarioService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SnapshotInventarioService> _logger;

        public SnapshotInventarioService(
            IServiceScopeFactory scopeFactory,
            ILogger<SnapshotInventarioService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SnapshotInventarioService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                var ahora     = DateTime.Now;
                var siguiente = ahora.Date.AddDays(1).AddMinutes(5); // mañana 00:05
                var espera    = siguiente - ahora;

                _logger.LogInformation(
                    "Próximo snapshot de inventario: {siguiente}", siguiente);

                await Task.Delay(espera, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await CalcularSnapshot();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al calcular snapshot de inventario");
                }
            }
        }

        private async Task CalcularSnapshot()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            var fecha = DateOnly.FromDateTime(DateTime.Today);

            await db.ExecuteAsync(
                "EXEC dbo.sp_calcular_snapshot_inventario @fecha, @sedeId",
                new { fecha = fecha.ToString("yyyy-MM-dd"), sedeId = 0 },
                commandTimeout: 120);

            _logger.LogInformation(
                "Snapshot de inventario calculado para {fecha}", fecha);
        }
    }
}
