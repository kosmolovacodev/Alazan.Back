using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace SistemaAlazan.Models // <--- Asegúrate que este nombre sea el mismo del 'using'
{
public class AnalisisCalidadDto
    {
        [JsonPropertyName("bascula_id")]
        public int BasculaId { get; set; }

        [JsonPropertyName("calibre")]
        public string Calibre { get; set; }

        [JsonPropertyName("humedad")]
        public decimal Humedad { get; set; }

        [JsonPropertyName("impurezas")]
        public decimal Impurezas { get; set; }

        [JsonPropertyName("r1_danado_insecto")]
        public decimal R1DanadoInsecto { get; set; }

        [JsonPropertyName("r2_quebrado")]
        public decimal R2Quebrado { get; set; }

        [JsonPropertyName("r2_manchado")]
        public decimal R2Manchado { get; set; }

        [JsonPropertyName("r2_arrugado")]
        public decimal R2Arrugado { get; set; }

        [JsonPropertyName("suma_r2")]
        public decimal SumaR2 { get; set; }

        [JsonPropertyName("total_danos")]
        public decimal TotalDanos { get; set; }

        [JsonPropertyName("analista_usuario_id")]
        public int AnalistaUsuarioId { get; set; }

        [JsonPropertyName("observaciones")]
        public string Observaciones { get; set; }

        [JsonPropertyName("datos_adicionales")]
        public string? DatosAdicionales { get; set; }

        [JsonPropertyName("sede_id")]
        public int SedeId { get; set; }

        [JsonPropertyName("grano_id")]
        public int? GranoId { get; set; }
    }
}