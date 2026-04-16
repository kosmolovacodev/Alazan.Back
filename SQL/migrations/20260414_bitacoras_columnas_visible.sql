-- Agrega columna 'visible' a bitacoras_columnas
-- visible = 0 oculta la columna en la UI sin borrar la fila (recuperable)
ALTER TABLE dbo.bitacoras_columnas
    ADD visible BIT NOT NULL DEFAULT 1;
GO

-- Ocultar columna "fecha" de FO-HC-IMP-003 (Análisis de Grano)
UPDATE dbo.bitacoras_columnas
SET visible = 0
WHERE codigo_bitacora = 'FO-HC-IMP-003'
  AND campo = 'fecha';
