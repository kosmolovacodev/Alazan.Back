using System;
using System.Collections.Generic;
namespace SistemaAlazan.Models // <--- Asegúrate que este nombre sea el mismo del 'using'
{

public class BasculaRecepcion
{
    public int id { get; set; }
    public string ticket_numero { get; set; } // SQL: varchar
    public string? boleta_numero { get; set; } // SQL: varchar
    public DateTimeOffset fecha_hora { get; set; } = DateTimeOffset.Now; // SQL: datetimeoffset
    public long productor_id { get; set; } // SQL: bigint
    public string? chofer { get; set; } // SQL: text
    public string? placas { get; set; } // SQL: text
    public decimal peso_bruto_kg { get; set; } // SQL: numeric
    public decimal? tara_kg { get; set; } // SQL: numeric
    public decimal? peso_neto_kg { get; set; } // SQL: numeric
    public int grano_id { get; set; } // SQL: int
    public int? origen_id { get; set; } // SQL: int
    public Guid? comprador_id { get; set; } // SQL: uniqueidentifier
    public string? status { get; set; } // SQL: varchar
    public string? datos_adicionales { get; set; } // SQL: nvarchar(max) (Aquí puedes guardar las Observaciones/Atiende)
    public long? usuario_registro_id { get; set; } // SQL: bigint
    public int sede_id { get; set; } // SQL: int - ID de la sede a la que pertenece este registro
}

public class ProductorNuevoReq {
    public string nombre { get; set; }
    public string telefono { get; set; }
    public string tipo_persona { get; set; }
    public string? atiende { get; set; } // Permitimos nulo para Personas Físicas
    public string? rfc { get; set; }
}
}