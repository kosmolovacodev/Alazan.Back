public class ReglasRecepcionDto {
    public int Id { get; set; } = 1;
    public int SedeId { get; set; }
    public decimal FactorImpurezas { get; set; }
    public bool AsignacionAutoSilo { get; set; }

    public bool ReglaTipoGrano { get; set; }
    public bool ReglaCalibre { get; set; }
    public bool ReglaExportacion { get; set; }
    public bool ReglaCapacidad { get; set; }

    public int AlertaCapacidadPct { get; set; }
    public bool SolicitarAprobacionProd { get; set; }
    public string? PreguntaProductor { get; set; }
    public string? AccionSiAcepta { get; set; }
    public string? AccionSiRechaza { get; set; }
    public bool SolicitarMotivoRechazo { get; set; }
    public bool RequerirFirmaDigital { get; set; }
    public bool ValAtiendeMorales { get; set; }
    public bool ValMultiplesEntregas { get; set; }
    public bool ValBloquearPlacas { get; set; }
    public bool ValLecturaAutoBascula { get; set; }
    public bool ValCapturaManualPeso { get; set; }
    public bool ValMotivoPesoManual { get; set; }
    public string? TplEjidal { get; set; }
    public string? TplPequenaPropiedad { get; set; }
    public string? TplPersonaMoral { get; set; }
}

public class ConfiguracionCampoDto {
    public int Id { get; set; }
    public string Pantalla { get; set; } = string.Empty;
    public string ClaveCampo { get; set; } = string.Empty;
    public string NombreMostrar { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Visible { get; set; }
    public bool Obligatorio { get; set; }
    public bool EsSistema { get; set; }
    public string? Descripcion { get; set; }
}