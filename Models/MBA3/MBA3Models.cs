using System.Text.Json;

public class TramaMba3DTO
{
    public string operacion { get; set; }
    public string codigo_empresa { get; set; }
    public string codigo_sucursal { get; set; }
    public string numero_orden { get; set; }
    public string fecha_ajuste { get; set; }
    public string codigo_bodega_materia_prima { get; set; }
    public string codigo_bodega_productos_proceso { get; set; }
    public string codigo_bodega_producto_terminado { get; set; }
    public string tipo_orden { get; set; }
    public string prioridad_orden { get; set; }
    public string estatus_orden { get; set; }
    public string documento_control { get; set; }
    public string numero_lote { get; set; }
    public string unidad_control { get; set; }
    public string numero_pedido { get; set; }
    public string fecha_requerida_interna { get; set; }
    public string fecha_requerida_cliente { get; set; }
    public string fecha_programada_inicio { get; set; }
    public string fecha_estimada_entrega { get; set; }
    public string fecha_real_inicio { get; set; }
    public string hora_requerida_interna { get; set; }
    public string hora_requerida_cliente { get; set; }
    public string hora_programada_inicio { get; set; }
    public string hora_real_inicio { get; set; }
    public string codigo_cliente_pedido { get; set; }
    public string codigo_producto_terminado { get; set; }
    public string ruta_produccion { get; set; }
    public decimal cantidad { get; set; }
    public decimal cantidad_segunda_unidad { get; set; }
    public string multidimension { get; set; }
    public string lote_ubicacion_pro_terminado { get; set; }
    public string notas_orden { get; set; }
    public string memo_general { get; set; }
    public string memo_bodega { get; set; }
    public string memo_responsable { get; set; }
    public string memo_avance { get; set; }
    public List<DetalleOrdenDTO> detalle { get; set; }
}

public class DetalleOrdenDTO
{
    public string codigo_producto_receta { get; set; }
    public string tipo_producto { get; set; }
    public string aplicacion_producto { get; set; }
    public decimal cantidad_producto { get; set; }
    public decimal cantidad_producto_segunda_uni { get; set; }
    public string listado_lotes { get; set; }
    public string listado_seriales { get; set; }
    public string listado_empaques { get; set; }
    public string multidimension { get; set; }
    public string memo { get; set; }
}
