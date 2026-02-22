public class TramaRecepcionDTO
{
   public string operacion { get; set; }
    public string codigo_empresa { get; set; }
    public string codigo_sucursal { get; set; }
    public string numero_documento { get; set; }
    public string codigo_proveedor { get; set; }
    public string fecha_recepcion { get; set; }
    public string sucursal_origen { get; set; }
    public string codigo_bodega { get; set; }
    public string memo { get; set; }
    public string hora_recepcion { get; set; }
    public string nombre_embarcacion { get; set; }
    public string procedencia { get; set; }
    public string codigo_calificador { get; set; }
    public string recepcion_externa { get; set; }
    public string chofer { get; set; }
    public string codigo_supervisor { get; set; }
    
    public string cinco_campos_adicionales_alpha { get; set; } = string.Empty;
    public string cinco_campos_adicionales_date { get; set; } = string.Empty;
    public string cinco_campos_adicionales_real { get; set; } = string.Empty;

    public List<DetalleRecepcionDTO> detalle { get; set; }
}

public class DetalleRecepcionDTO
{
    public string codigo_producto { get; set; }
    public decimal cantidad { get; set; }
    public string serial { get; set; }
    public string datos_lote { get; set; }
    public string datos_pedimento { get; set; }
    public string datos_estado_calificacion { get; set; }
    public string datos_peso_embarque_memo { get; set; }
}