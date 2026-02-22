using System;

namespace Alazan.API.Models
{
    // DTO para boleta de precio (para mostrar en la lista)
    public class BoletaPrecioDto
    {
        public int Id { get; set; }
        public string NoBoleta { get; set; } = string.Empty;
        public string? Ticket { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string? Comprador { get; set; }
        public string? Productor { get; set; }
        public string? Telefono { get; set; }
        public string? Origen { get; set; }
        public string? Calibre { get; set; }
        public decimal? PesoBruto { get; set; }
        public decimal TonsAprox { get; set; }
        public decimal Descuento { get; set; }
        public decimal PrecioSugerido { get; set; }
        public string? PrecioSugeridoCodigo { get; set; }
        public decimal? PrecioAutorizado { get; set; }
        public decimal? PrecioFinal { get; set; }
        public string Estatus { get; set; } = string.Empty;
        public DateTimeOffset? TiempoRegistro { get; set; }
        public bool EsDeAnalisis { get; set; }
    }

    // DTO para autorizar un precio
    public class AutorizarPrecioDto
    {
        public int BoletaPrecioId { get; set; }
        public decimal PrecioAutorizado { get; set; }
        public string? Observaciones { get; set; }
        public bool AutorizacionAutomatica { get; set; } = false;
        // CG = precio default/sugerido, CC = precio superior (requiere justificación), NORMAL = precio inferior
        public string? TipoAutorizacion { get; set; }
    }

    // DTO para renegociar un precio
    public class RenegociarPrecioDto
    {
        public int BoletaPrecioId { get; set; }
        public decimal PrecioNuevo { get; set; }
        public string MotivoRenegociacion { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    // DTO para rechazar un precio
    public class RechazarPrecioDto
    {
        public int BoletaPrecioId { get; set; }
        public string MotivoRechazo { get; set; } = string.Empty;
    }

    // DTO para configuración de precios
    public class ConfiguracionPrecioDto
    {
        public int Id { get; set; }
        public int SedeId { get; set; }
        public bool HabilitarAutorizacionAutomatica { get; set; }
        public int MinutosParaAutorizacion { get; set; }
        public decimal? ToleranciaPrecioPct { get; set; }
        public bool RequiereAutorizacionFueraTolerancia { get; set; }
    }

    // DTO para historial de cambios de precio
    public class HistorialPrecioDto
    {
        public int Id { get; set; }
        public string NoBoleta { get; set; } = string.Empty;
        public decimal? PrecioAnterior { get; set; }
        public decimal PrecioNuevo { get; set; }
        public string? MotivoCambio { get; set; }
        public string TipoAccion { get; set; } = string.Empty;
        public string? Usuario { get; set; }
        public DateTimeOffset FechaCambio { get; set; }
    }
}
