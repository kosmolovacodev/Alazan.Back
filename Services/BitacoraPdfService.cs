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
        public List<string>                         Campos      { get; set; } = new(); // nombres reales de campo
        public List<Dictionary<string, string>>     Filas       { get; set; } = new();
        public List<(string Rol, string Nombre)>    SlotsFirma  { get; set; } = new();
    }

    public class BitacoraDocumentoFirmaPdf
    {
        public string  Etiqueta       { get; set; } = "";
        public bool    Firmado        { get; set; }
        public string? NombreFirmante { get; set; }
        public string? FirmadoEn     { get; set; }
    }

    public class BitacoraDocumentoPdfData
    {
        public int    DocumentoId    { get; set; }
        public int    SedeId         { get; set; }
        public string CodigoBitacora { get; set; } = "";
        public string NombreBitacora { get; set; } = "";
        public string Seccion        { get; set; } = "";
        public string Sede           { get; set; } = "";
        public string Fecha          { get; set; } = "";
        public string GeneradoPor    { get; set; } = "";
        public string NombreEmpresa  { get; set; } = "";
        public string Rfc            { get; set; } = "";
        public List<string>                             Encabezados { get; set; } = new();
        public List<string>                             Campos      { get; set; } = new(); // nombres reales de campo
        public List<Dictionary<string, string>>         Filas       { get; set; } = new();
        public List<BitacoraDocumentoFirmaPdf>          Firmas      { get; set; } = new();
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
                                    for (int j = 0; j < data.Encabezados.Count; j++)
                                    {
                                        var campo = j < data.Campos.Count
                                            ? data.Campos[j].ToLowerInvariant()
                                            : data.Encabezados[j].ToLower().Replace(" ", "_");
                                        var val = fila.TryGetValue(campo, out var v) ? v ?? "" : "";
                                        table.Cell().Background(bg).Padding(3).Text(val).FontSize(8);
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

        // Genera PDF para un documento oficial — misma estructura que el modal "Ver Documento"
        public string GenerarDocumento(BitacoraDocumentoPdfData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var dir = Path.Combine(_env.ContentRootPath, "bitacoras-pdfs", data.SedeId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = $"doc_{data.DocumentoId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(dir, fileName);

            var empresa = !string.IsNullOrWhiteSpace(data.NombreEmpresa)
                ? data.NombreEmpresa
                : "SISTEMA ALAZAN";

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── ENCABEZADO (igual al modal HTML) ─────────────────────
                        col.Item().BorderBottom(2).BorderColor("#1a2233").PaddingBottom(8).Row(row =>
                        {
                            // Centro: empresa + RFC + nombre bitácora
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text(empresa).Bold().FontSize(13).FontColor("#1a2233");
                                if (!string.IsNullOrWhiteSpace(data.Rfc))
                                    c.Item().AlignCenter().Text($"RFC: {data.Rfc}").FontSize(8).FontColor("#555");
                                c.Item().AlignCenter().Text(data.NombreBitacora).Bold().FontSize(11);
                            });
                            // Derecha: Código + Fecha
                            row.ConstantItem(170).AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Código: {data.CodigoBitacora}").FontSize(8);
                                c.Item().AlignRight().Text($"Fecha: {data.Fecha}").FontSize(8);
                                if (!string.IsNullOrWhiteSpace(data.Sede))
                                    c.Item().AlignRight().Text($"Sede: {data.Sede}").FontSize(8);
                                c.Item().AlignRight().Text($"Generado por: {data.GeneradoPor}").FontSize(7).FontColor("#555");
                            });
                        });

                        col.Item().Height(8);

                        // ── TABLA DE DATOS ────────────────────────────────────────
                        if (data.Filas.Count > 0 && data.Encabezados.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(def =>
                                {
                                    def.ConstantColumn(24);
                                    foreach (var _ in data.Encabezados)
                                        def.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background("#1a2233").Padding(4)
                                        .Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                                    foreach (var enc in data.Encabezados)
                                        h.Cell().Background("#1a2233").Padding(4)
                                            .Text(enc).FontColor("#FFFFFF").Bold().FontSize(8);
                                });

                                for (int i = 0; i < data.Filas.Count; i++)
                                {
                                    var fila = data.Filas[i];
                                    string bg = i % 2 == 0 ? "#FFFFFF" : "#f5f5f5";
                                    table.Cell().Background(bg).Padding(3)
                                        .Text((i + 1).ToString()).FontSize(8);
                                    for (int j = 0; j < data.Encabezados.Count; j++)
                                    {
                                        var campo = j < data.Campos.Count
                                            ? data.Campos[j].ToLowerInvariant()
                                            : data.Encabezados[j].ToLower().Replace(" ", "_");
                                        var val = fila.TryGetValue(campo, out var v) ? v ?? "" : "";
                                        table.Cell().Background(bg).Padding(3).Text(val).FontSize(8);
                                    }
                                }
                            });
                        }
                        else
                        {
                            col.Item().Padding(12).Text("Sin registros en este período.").FontColor("#999");
                        }

                        col.Item().Height(16);

                        // ── TABLA DE FIRMAS (igual al modal HTML: 4 columnas) ─────
                        col.Item().Table(firmTable =>
                        {
                            firmTable.ColumnsDefinition(def =>
                            {
                                def.RelativeColumn(2.5f); // Puesto / Área
                                def.RelativeColumn(1.5f); // Fecha
                                def.RelativeColumn(2.5f); // Nombre
                                def.RelativeColumn(2.5f); // Firma
                            });

                            // Header: título y subencabezados
                            firmTable.Header(h =>
                            {
                                h.Cell().ColumnSpan(4).Background("#eeeeee")
                                    .Border(0.5f).BorderColor("#bbbbbb")
                                    .Padding(4).AlignCenter()
                                    .Text("FIRMAS DE CONTROL").Bold().FontSize(8).FontColor("#1a2233");

                                foreach (var lbl in new[] { "Puesto / Área", "Fecha", "Nombre", "Firma" })
                                    h.Cell().Background("#f5f5f5")
                                        .Border(0.5f).BorderColor("#bbbbbb")
                                        .Padding(4).Text(lbl).Bold().FontSize(7);
                            });

                            if (data.Firmas.Count == 0)
                            {
                                firmTable.Cell().ColumnSpan(4).Padding(8)
                                    .Text("Sin slots de firma configurados").FontSize(8).FontColor("#999");
                            }
                            else
                            {
                                foreach (var firma in data.Firmas)
                                {
                                    firmTable.Cell().Border(0.5f).BorderColor("#bbbbbb")
                                        .Padding(4).Text(firma.Etiqueta).FontSize(8);

                                    firmTable.Cell().Border(0.5f).BorderColor("#bbbbbb")
                                        .Padding(4).Text(firma.FirmadoEn ?? "").FontSize(8);

                                    firmTable.Cell().Border(0.5f).BorderColor("#bbbbbb")
                                        .Padding(4).Text(firma.Firmado ? (firma.NombreFirmante ?? "") : "").FontSize(8);

                                    if (firma.Firmado)
                                        firmTable.Cell().Border(0.5f).BorderColor("#10b981")
                                            .Padding(4).AlignCenter().AlignMiddle()
                                            .Text("Firmado ✓").FontSize(8).FontColor("#10b981").Bold();
                                    else
                                        firmTable.Cell().Border(0.5f).BorderColor("#bbbbbb")
                                            .Padding(4)
                                            .Text("Pendiente").FontSize(7).FontColor("#e65100");
                                }
                            }
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
            _logger.LogInformation("PDF documento generado: {Path}", filePath);
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
