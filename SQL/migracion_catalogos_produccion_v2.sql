-- =============================================================================
-- Migración: calibres OZ AM / OZ ESP en calibres_catalogo
-- Ejecutar UNA sola vez
-- =============================================================================

-- 1. Ampliar la UNIQUE constraint para permitir mismo calibre_mm
--    en distintos grano_id / clasificacion / sede_id
ALTER TABLE dbo.calibres_catalogo
    DROP CONSTRAINT UQ_calibres_mm;
GO

ALTER TABLE dbo.calibres_catalogo
    ADD CONSTRAINT UQ_calibres_mm
    UNIQUE (calibre_mm, grano_id, clasificacion, sede_id);
GO

-- 2. Asignar clasificacion = 'OZ AM' a calibres Garbanzo que aún tienen NULL
UPDATE dbo.calibres_catalogo
SET clasificacion = 'OZ AM'
WHERE grano_id = 4
  AND activo = 1
  AND (clasificacion IS NULL OR clasificacion = '');
GO

-- 3. Insertar calibres OZ ESP de Garbanzo (sede_id 8, grano_id 4)
INSERT INTO dbo.calibres_catalogo
    (calibre_mm, descuento_default_kg_ton, clasificacion, activo, sede_id, grano_id)
SELECT val.calibre_mm, 0.00, 'OZ ESP', 1, 8, 4
FROM (VALUES ('36/38'), ('42/44'), ('44/46'), ('46/48'), ('48/50')) AS val(calibre_mm)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.calibres_catalogo
    WHERE calibre_mm = val.calibre_mm AND grano_id = 4
      AND clasificacion = 'OZ ESP' AND sede_id = 8
);
GO

-- 4. Insertar calibres OZ AM de Garbanzo faltantes (sede_id 8, grano_id 4)
INSERT INTO dbo.calibres_catalogo
    (calibre_mm, descuento_default_kg_ton, clasificacion, activo, sede_id, grano_id)
SELECT val.calibre_mm, 0.00, 'OZ AM', 1, 8, 4
FROM (VALUES ('34/36'), ('40/42'), ('42/44'), ('44/46'), ('46/48'), ('48/52')) AS val(calibre_mm)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.calibres_catalogo
    WHERE calibre_mm = val.calibre_mm AND grano_id = 4
      AND clasificacion = 'OZ AM' AND sede_id = 8
);
GO

-- 5. Verifica resultado
SELECT c.id, c.calibre_mm, c.clasificacion, g.nombre AS grano, c.sede_id
FROM dbo.calibres_catalogo c
JOIN dbo.granos_catalogo g ON g.id = c.grano_id
ORDER BY g.nombre, c.clasificacion, c.calibre_mm;
GO
