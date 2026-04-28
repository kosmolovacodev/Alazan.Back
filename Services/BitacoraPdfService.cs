using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Alazan.API.Services
{
    public class BitacoraPdfData
    {
        public int    RegistroId      { get; set; }
        public int    SedeId          { get; set; }
        public string CodigoBitacora  { get; set; } = "";
        public string NombreBitacora  { get; set; } = "";
        public string Seccion         { get; set; } = "";
        public string Sede            { get; set; } = "";
        public string Fecha           { get; set; } = "";
        public string GeneradoPor     { get; set; } = "";
        public List<string>                         Encabezados { get; set; } = new();
        public List<Dictionary<string, string>>     Filas       { get; set; } = new();
        public List<(string Rol, string Nombre)>    SlotsFirma  { get; set; } = new();
    }

    public class BitacoraPdfService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BitacoraPdfService> _logger;

        public BitacoraPdfService(IWebHostEnvironment env, ILogger<BitacoraPdfService> logger)
        {
            _env = env;
            _logger = logger;
        }

        // Genera el PDF y retorna la ruta relativa (para URL pública)
        public string Generar(BitacoraPdfData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var dir = Path.Combine(_env.ContentRootPath, "bitacoras-pdfs", data.SedeId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = $"{data.RegistroId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(dir, fileName);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── ENCABEZADO ────────────────────────────────────────────
                        col.Item().BorderBottom(1).BorderColor("#1a2233").PaddingBottom(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("SISTEMA ALAZAN").Bold().FontSize(14).FontColor("#1a2233");
                                c.Item().Text($"Bitácora: {data.NombreBitacora}").Bold().FontSize(11);
                                c.Item().Text($"Código: {data.CodigoBitacora}  |  Sección: {data.Seccion}");
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Sede: {data.Sede}").AlignRight();
                                c.Item().Text($"Fecha: {data.Fecha}").AlignRight();
                                c.Item().Text($"Generado por: {data.GeneradoPor}").AlignRight().FontColor("#555");
                            });
                        });

                        col.Item().Height(8);

                        // ── TABLA DE DATOS ────────────────────────────────────────
                        if (data.Filas.Count > 0 && data.Encabezados.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                // Columna numeración
                                table.ColumnsDefinition(def =>
                                {
                                    def.ConstantColumn(24);
                                    foreach (var _ in data.Encabezados)
                                        def.RelativeColumn();
                                });

                                // Encabezados
                                table.Header(h =>
                                {
                                    h.Cell().Background("#1a2233").Padding(4)
                                        .Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                                    foreach (var enc in data.Encabezados)
                                        h.Cell().Background("#1a2233").Padding(4)
                                            .Text(enc).FontColor("#FFFFFF").Bold().FontSize(8);
                                });

                                // Filas
                                for (int i = 0; i < data.Filas.Count; i++)
                                {
                                    var fila = data.Filas[i];
                                    string bg = i % 2 == 0 ? "#FFFFFF" : "#f5f5f5";

                                    table.Cell().Background(bg).Padding(3)
                                        .Text((i + 1).ToString()).FontSize(8);
                                    foreach (var enc in data.Encabezados)
                                    {
                                        var val = fila.TryGetValue(enc.ToLower().Replace(" ", "_"), out var v) ? v ?? "" : "";
                                        table.Cell().Background(bg).Padding(3)
                                            .Text(val).FontSize(8);
                                    }
                                }
                            });
                        }
                        else
                        {
                            col.Item().Padding(12).Text("Sin registros en esta bitácora.").FontColor("#999");
                        }

                        col.Item().Height(20);

                        // ── ZONA DE FIRMAS ────────────────────────────────────────
                        col.Item().BorderTop(1).BorderColor("#ccc").PaddingTop(10).Column(firmCol =>
                        {
                            firmCol.Item().Text("FIRMAS Y AUTORIZACIONES")
                                .Bold().FontSize(9).FontColor("#1a2233");
                            firmCol.Item().Height(8);

                            firmCol.Item().Row(firmRow =>
                            {
                                foreach (var slot in data.SlotsFirma)
                                {
                                    firmRow.RelativeItem().Column(slotCol =>
                                    {
                                        slotCol.Item().Height(40).BorderBottom(1).BorderColor("#333");
                                        slotCol.Item().PaddingTop(4).Text(slot.Nombre).Bold().FontSize(8);
                                        slotCol.Item().Text($"Cargo: {CapitalizarRol(slot.Rol)}").FontSize(7).FontColor("#555");
                                        slotCol.Item().Text("Fecha: ___/___/___").FontSize(7).FontColor("#555");
                                    });
                                    firmRow.ConstantItem(20);
                                }
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ").FontSize(7).FontColor("#999");
                        t.CurrentPageNumber().FontSize(7).FontColor("#999");
                        t.Span(" de ").FontSize(7).FontColor("#999");
                        t.TotalPages().FontSize(7).FontColor("#999");
                    });
                });
            });

            doc.GeneratePdf(filePath);
            _logger.LogInformation("PDF generado: {Path}", filePath);

            // Retorna la ruta relativa bajo bitacoras-pdfs/ (sin el prefijo)
            return $"{data.SedeId}/{fileName}";
        }

        private static string CapitalizarRol(string rol) => rol switch
        {
            "operativo"  => "Operativo",
            "supervisor" => "Supervisor",
            "gerente"    => "Gerente",
            "recepcion"  => "Recepción",
            _            => rol
        };
    }
}
