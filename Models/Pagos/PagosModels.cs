using System;
using System.Collections.Generic;

namespace SistemaAlazan.Models
{
    // ─── Fila de la tabla de solicitudes de pago ─────────────────────────────
    public class SolicitudPagoListItem
    {
        // ─── solicitudes_pago ───────────────────────────────────────────────
        public int    Id               { get; set; }
        public int    FacturacionId    { get; set; }
        public decimal MontoSolicitado  { get; set; }
        public string Status           { get; set; }   // SOLICITAR | PAGO SOLICITADO | AUTORIZADO | PAGADO
        public DateTime  CreatedAt     { get; set; }
        public DateTime? FechaSolicitud    { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public DateTime? FechaPago         { get; set; }
        public string    FolioPago         { get; set; }

        // ─── Banco / forma de pago asignados a la solicitud ─────────────────
        public int?   BancoId         { get; set; }
        public string NombreBanco     { get; set; }
        public int?   FormaPagoId     { get; set; }
        public string NombreFormaPago { get; set; }
        public string Clabe           { get; set; }
        public string Cuenta          { get; set; }

        // ─── Datos del ticket (boleta + facturacion_recepciones) ─────────────
        public string   Ticket              { get; set; }
        public DateTime? FechaEntrega        { get; set; }
        public string   RfcProductor        { get; set; }
        public decimal  KgTotalEntregados   { get; set; }
        public decimal  PrecioPromedio      { get; set; }
        public decimal  ImporteFactura      { get; set; }

        // ─── Datos del productor ─────────────────────────────────────────────
        public string NombreProductor       { get; set; }
        public string CuentaClabeProductor  { get; set; }   // CLABE registrada en productores
        public int?   BancoIdProductor      { get; set; }
        public string BancoProductor        { get; set; }

        // ─── Sede ───────────────────────────────────────────────────────────
        public int    SedeId     { get; set; }
        public string NombreSede { get; set; }
    }

    // ─── Request: SOLICITAR → PAGO SOLICITADO ────────────────────────────────
    public class SolicitarPagoRequest
    {
        public int[] SolicitudIds { get; set; }
        public int   SedeId       { get; set; }
    }

    // ─── Request: actualizar banco/CLABE antes de solicitar ─────────────────
    public class ActualizarDatosBancariosRequest
    {
        public int    SolicitudId  { get; set; }
        public int?   BancoId      { get; set; }
        public int?   FormaPagoId  { get; set; }
        public string Clabe        { get; set; }
        public string Cuenta       { get; set; }
        public int    SedeId       { get; set; }
    }

    // ─── Request: PAGO SOLICITADO → AUTORIZADO ───────────────────────────────
    public class AutorizarPagosRequest
    {
        public int[]    SolicitudIds      { get; set; }
        public DateTime FechaAutorizacion { get; set; }
        public int      SedeId            { get; set; }
    }

    // ─── Request: AUTORIZADO → PAGADO ────────────────────────────────────────
    public class RegistrarPagoRequest
    {
        public int     SolicitudId  { get; set; }
        public DateTime FechaPago   { get; set; }
        public string  FolioPago    { get; set; }
        public int?    BancoId      { get; set; }
        public int?    FormaPagoId  { get; set; }
        public string  Cuenta       { get; set; }
        public decimal ImportePago  { get; set; }
        public int     SedeId       { get; set; }
    }
}
