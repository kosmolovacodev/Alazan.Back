using System;

namespace Alazan.API.Models
{
    // --- CATÁLOGOS BASE ---
    public record Rol(long Id, string NombreRol, string? Descripcion, string? PermisosJson, bool Activo);
    public record Usuario(long Id, Guid? AuthUserId, string NombreCompleto, string Email, long? RolId, string? Departamento, bool Activo);
    public record Productor(long Id, 
    string Nombre, 
    string? Rfc, 
    long? OrigenId, 
    long? BancoId, 
    bool Activo);
    public record Grano(long Id, string Nombre, string? Variedad, string UnidadMedida, bool Activo);
    public record Calibre(long Id, int CalibreMm, string? Descripcion, decimal DescuentoDefault, string? Clasificacion);
    public record Origen(long Id, string Municipio, string Estado, string? Region);
    public record Banco(long Id, string NombreBanco, string? CodigoBanco);
    public record BodegaSilo(long Id, string NombreBodega, string? NumeroSilo, decimal CapacidadTon);
    public record Sede(long Id, string NombreSede, string Ciudad, string Estado, decimal TopeDiario, bool Activo);

    // --- TABLAS OPERATIVAS ---
    //public record BasculaRecepcion(long Id, string TicketNumero, DateTimeOffset FechaHora, long ProductorId, decimal PesoBrutoKg, decimal TaraKg, decimal PesoNetoKg, string Status);
    public record AnalisisCalidad(long Id, long BasculaId, string? Calibre, decimal Humedad, decimal Impurezas, decimal TotalDanos, string? Observaciones);
    public record PrecioConfig(long Id, DateTime FechaVigencia, decimal PrecioBaseUsdTon, decimal TipoCambioFix, decimal PrecioBaseMxnTon, bool Activo);
    public record Boleta(long Id, string Folio, long BasculaId, long PrecioConfigId, decimal KgALiquidar, decimal ImporteTotalMxn, string Status);
    public record VolcadoBodega(long Id, long BasculaId, long BodegaId, DateTimeOffset FechaHoraVolcado, decimal KgVolcados);
    public record FacturacionRecepcion(long Id, long ProductorId, Guid? UuidFiscal, string? Serie, string? Folio, decimal MontoTotal, string Status);
    public record SolicitudPago(long Id, long FacturacionId, decimal MontoSolicitado, string Prioridad, string Status);
    public record TipoCambioHistorico(long Id, DateTime Fecha, decimal ValorFix);
    public record CuentaBancariaEmpresa(long Id, long BancoId, string NumeroCuenta, string Clabe, string Moneda);
    public record DocumentoExpediente(long Id, long ProductorId, string TipoDocumento, string RutaArchivo, DateTime? FechaVencimiento, bool Validado);

    // --- CONFIGURACIÓN ---
    public record ParametroGeneral(long Id, string Clave, string Valor, string? Descripcion, string? Categoria);
    public record FolioConsecutivo(long Id, string TipoDocumento, string? Prefijo, int UltimoValor, int LongitudFolio);
    public record ConfigBascula(long Id, long SedeId, string? PuertoCom, int BaudRate, string UnidadMedida);
    public record ConfigAnalisis(long Id, long GranoId, string ParametroNombre, decimal ValorBase, decimal ValorMaximo, decimal DescuentoPorPunto);
    public record ConfigDocumentoExpediente(long Id, string TipoDocumento, string NombreDocumento, bool EsObligatorio, int? VigenciaDias);
    public record ConfigFacturacion(long Id, bool ValidarXmlSat, int DiasTolerancia, decimal MontoMinimo, bool RequiereExpedienteCompleto);
    public record ConfigPago(long Id, string MetodoPagoDefault, bool RequiereAprobacionDoble, decimal MontoLimiteSimple);
}