using System;
using System.Collections.Generic;
namespace SistemaAlazan.Models // <--- Asegúrate que este nombre sea el mismo del 'using'
{
    public class FacturacionConfigDto
    {
        public FacturacionGeneralDto General { get; set; }
        public List<FacturacionDocumentoDto> Documentos { get; set; }
        public List<FacturacionStatusDto> StatusFlujo { get; set; }
        public FacturacionEndosoDto Endoso { get; set; }
        public List<FacturacionEndosoClausulaDto> ClausulasEndoso { get; set; }
        public List<FacturacionEndosoDocDto> DocumentosEndoso { get; set; }
    }

    public class FacturacionGeneralDto
    {
        public int Id { get; set; }
        public bool ValidarRfcSat { get; set; }
        public int DiasAlertaVencimiento { get; set; }
        public bool PermitirDocsVencidos { get; set; }
        public bool ValidarFormatoArchivos { get; set; }
        public int TamanoMaximoMb { get; set; }
        public decimal RetencionFederalPct { get; set; }
        public decimal RetencionIsrPct { get; set; }
        public decimal RetencionIvaPct { get; set; }
        public bool AplicarRetencionAuto { get; set; }
        public int LongitudRfcFisica { get; set; }
        public int LongitudRfcMoral { get; set; }
        public bool RequiereActaMoral { get; set; }
        
        // Esta es la columna VARCHAR(MAX) de tu tabla Configuracion_Facturacion_General
        public string FormatosAceptados { get; set; } 
    }

    public class FacturacionDocumentoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Formato { get; set; }
        public bool Obligatorio { get; set; }
        public bool RequiereVigencia { get; set; }
        public int DiasVigenciaDefault { get; set; }
        public bool AplicaPersonaFisica { get; set; }
        public bool AplicaPersonaMoral { get; set; }
        public bool Activo { get; set; }
    }

    public class FacturacionStatusDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string ColorHex { get; set; }
        public string Descripcion { get; set; }
        public int Orden { get; set; }
        public bool BloqueaPago { get; set; }
        public bool Activo { get; set; }
    }

    public class FacturacionEndosoDto
    {
        public int Id { get; set; }
        public string TituloDocumento { get; set; }
        public bool RequiereEscaneado { get; set; }
        public bool RequiereInfoBeneficiario { get; set; }
        public string TextoParrafo1 { get; set; }
        public string TextoParrafo2 { get; set; }
        public string TextoParrafo3 { get; set; }
        public string TextoParrafo4 { get; set; }
    }

    public class FacturacionEndosoClausulaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class FacturacionEndosoDocDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public string Formato { get; set; }
        public bool Obligatorio { get; set; }
    }
}