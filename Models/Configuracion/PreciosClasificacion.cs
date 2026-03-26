public class PrecioClasificacionDto
{
    public int Id { get; set; }
    public int SedeId { get; set; }
    public int GranoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal PrecioKg { get; set; }
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
}
