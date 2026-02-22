namespace SistemaAlazan.Models
{
    public class FacturacionRecepcion
    {
        public int Id { get; set; }
        public long ProductorId { get; set; }
        public string? UuidFiscal { get; set; }
        public string? Serie { get; set; }
        public string? Folio { get; set; }
        public decimal MontoTotal { get; set; }
        public string Status { get; set; } = "PENDIENTE";
        public int SedeId { get; set; }
        public int? PreliquidacionId { get; set; }
        public int? BoletaId { get; set; }
        public int? EntregaAgrupadaId { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string? RfcProductor { get; set; }
        public string? XmlFacturaPath { get; set; }
        public decimal? ImporteFactura { get; set; }
        public decimal? KgTotalEntregados { get; set; }
        public decimal? PrecioPromedio { get; set; }
        public bool TieneDocumentos { get; set; }
        public bool TieneFacturaXml { get; set; }
        public long? UsuarioRegistroId { get; set; }
    }

    public class ConfigFacturacionGeneral
    {
        public int Id { get; set; }
        public decimal RetencionIsrPct { get; set; }
        public int LongitudRfcFisica { get; set; }
        public int LongitudRfcMoral { get; set; }
        public string? FormatosAceptados { get; set; }
    }

    public class FacturacionStatus
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public int BloqueaPago { get; set; }
    }

    public class FacturacionDocumentoConfig
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Formato { get; set; } = string.Empty;
        public int Obligatorio { get; set; }
    }

    // ── DTOs de Request ──

    public class ActualizarRfcRequest
    {
        public string[] Tickets { get; set; } = Array.Empty<string>();
        public string NuevoRfc { get; set; } = string.Empty;
        public int SedeId { get; set; }
    }

    public class ActualizarDocsStatusRequest
    {
        public string[] Tickets { get; set; } = Array.Empty<string>();
        public bool TieneDocumentos { get; set; }
        public bool TieneFacturaXml { get; set; }
        public int SedeId { get; set; }
    }

    public class EnviarAPagosRequest
    {
        public string[] Tickets { get; set; } = Array.Empty<string>();
        public int SedeId { get; set; }
    }

    public class GuardarXmlRequest
    {
        public string[] Tickets { get; set; } = Array.Empty<string>();
        public int SedeId { get; set; }
        public string? XmlBase64 { get; set; }
        public string? XmlNombre { get; set; }
        public string? Importe { get; set; }
        public string? PagoPredial { get; set; }
        public string? DescPredial { get; set; }
        public string? DescISR { get; set; }
        public string? DiasHabilesPago { get; set; }
    }

    public class GuardarExpedienteRequest
    {
        public string[] Tickets { get; set; } = Array.Empty<string>();
        public int SedeId { get; set; }
        // Datos productor
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Origen { get; set; }
        public string? Municipio { get; set; }
        public string? Atiende { get; set; }
        // Datos facturación
        public string? Importe { get; set; }
        public string? PagoPredial { get; set; }
        public string? DescPredial { get; set; }
        public string? DescISR { get; set; }
        public string? DiasHabilesPago { get; set; }
        // Archivos Base64
        public string? XmlBase64 { get; set; }
        public string? XmlNombre { get; set; }
        public string? IdentificacionBase64 { get; set; }
        public string? IdentificacionNombre { get; set; }
        public string? ConstanciaBase64 { get; set; }
        public string? ConstanciaNombre { get; set; }
        public string? FechaConstancia { get; set; }
        public string? OpinionBase64 { get; set; }
        public string? OpinionNombre { get; set; }
        public string? FechaOpinion { get; set; }
        public string? ActaConstitutivaBase64 { get; set; }
        public string? ActaConstitutivaNombre { get; set; }
        public string? OtroBase64 { get; set; }
        public string? OtroNombre { get; set; }
    }

public class RegistroOcrDto
{
    public string TextoOcr { get; set; } = string.Empty;
    public string NombreProductorEsperado { get; set; } = string.Empty;
    public string ArchivoBase64 { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public int SedeId { get; set; }
    public string[] Tickets { get; set; } = Array.Empty<string>(); // <--- Añadir esto
}
}