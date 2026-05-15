-- ============================================================
--  Tarea 14.1: Timestamp de cierre de resultado de producción
-- ============================================================
ALTER TABLE dbo.resultado_produccion
    ADD fecha_resultado DATETIME NULL;
GO
