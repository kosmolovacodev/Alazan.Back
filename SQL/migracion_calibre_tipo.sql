-- Migración: agregar columna calibre_tipo a ordenesproduccion
-- Ejecutar UNA sola vez en la base de datos de producción

ALTER TABLE dbo.ordenesproduccion
    ADD calibre_tipo NVARCHAR(200) NULL;
GO

-- Formato que se guarda:
--   "OZ AM:34/36,40/42"            → solo OZ AM con esos calibres
--   "OZ ESP:42/44,44/46"           → solo OZ ESP con esos calibres
--   "OZ AM:34/36,40/42|OZ ESP:42/44,44/46"  → ambos tipos (equivalencias)
--   "FRIJOL:Hasta 70,71-75"        → calibres de frijol
