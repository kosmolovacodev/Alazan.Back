-- Quitar columna "Fecha" de la bitácora FO-HC-IMP-003 (Análisis de Grano)
DELETE FROM dbo.bitacoras_columnas
WHERE codigo_bitacora = 'FO-HC-IMP-003'
  AND campo = 'fecha';
