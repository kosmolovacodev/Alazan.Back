using System;
using System.Collections.Generic;
namespace SistemaAlazan.Models // <--- Asegúrate que este nombre sea el mismo del 'using'
{

// DTO Principal (Contenedor)
    public class PagosConfigDto
    {
        public PagosGeneralDto General { get; set; }
        public List<PagosStatusDto> Status { get; set; }
        public List<PagosFormaDto> FormasPago { get; set; }
        public List<PagosDiaDto> DiasHabiles { get; set; }
        public List<PagosSedeDto> TopesSede { get; set; }
    }

    public class PagosGeneralDto
    {
        public int Id { get; set; }
        // Horarios
        public string HorarioLimiteSolicitud { get; set; }
        public bool AlertaDiasFestivos { get; set; }
        // Tiempos
        public int DiasAutorizacion { get; set; }
        public int DiasEjecucion { get; set; }
        public int DiasAlertaVencimiento { get; set; }
        // Validaciones
        public bool ValidarTopesDiarios { get; set; }
        public bool ValidarDiasHabiles { get; set; }
        public bool ValidarHorarioLimite { get; set; }
        public bool RequiereFolioPago { get; set; }
        public bool RequiereComprobantePago { get; set; }
        public bool PermitirPagoParcial { get; set; }
        public decimal MontoMinimoPago { get; set; }
    }

    public class PagosStatusDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Color { get; set; }
        public string Descripcion { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public bool BloqueaEdicion { get; set; }
        public bool RequiereAprobacion { get; set; }
    }

    public class PagosFormaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public bool Activo { get; set; }
        public bool RequiereCLABE { get; set; }
        public bool RequiereCuenta { get; set; }
    }

    public class PagosDiaDto
    {
        public int Id { get; set; }
        public string Dia { get; set; }
        public bool Activo { get; set; }
    }

    public class PagosSedeDto
    {
        public int Id { get; set; }
        public string NombreSede { get; set; } // En C# usamos PascalCase
        public string Ciudad { get; set; }
        public string Estado { get; set; }
        public decimal TopeDiario { get; set; }
        public bool Activo { get; set; }
    }

}