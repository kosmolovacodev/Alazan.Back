public class DescuentoCalibre {
    public int Id { get; set; }
    public string Codigo { get; set; }
    public string Calibre { get; set; }
    public decimal descuento_kg_ton { get; set; }
    public int? grano_id { get; set; }
    public int? sede_id { get; set; }
}

public class DescuentoPrecio {
    public int Id { get; set; }
    public string Codigo { get; set; }
    public decimal descuento_mxn { get; set; }
    public int? grano_id { get; set; }
    public int? sede_id { get; set; }
}

public class NivelExportacion {
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? PorcentajeExportLabel { get; set; }
    public int? DescuentoPrecioId { get; set; }
    public bool Vigente { get; set; }
    public string? CodigoDescuento { get; set; }
    public decimal? ValorDescuento { get; set; }
    public decimal? PrecioFinalMxn { get; set; }
    public int? grano_id { get; set; }
    public int? sede_id { get; set; }
}

public class ConfigPrecioDto {
    public decimal PrecioBaseUsd { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal PrecioBaseMxn { get; set; } 
    public string UrlApi { get; set; }
    public string UsuarioRegistro { get; set; } 
    public DateTimeOffset? FechaRegistro { get; set; } 
    public int SedeId { get; set; }
}