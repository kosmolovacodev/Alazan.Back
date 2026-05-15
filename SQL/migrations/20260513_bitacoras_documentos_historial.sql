-- FASE 1F: Tarea 20 — Historial de documentos generados
-- Agrega generado_por y pdf_path a bitacoras_documentos

IF COL_LENGTH('dbo.bitacoras_documentos', 'generado_por') IS NULL
    ALTER TABLE dbo.bitacoras_documentos ADD generado_por NVARCHAR(200) NULL;

IF COL_LENGTH('dbo.bitacoras_documentos', 'pdf_path') IS NULL
    ALTER TABLE dbo.bitacoras_documentos ADD pdf_path NVARCHAR(500) NULL;
