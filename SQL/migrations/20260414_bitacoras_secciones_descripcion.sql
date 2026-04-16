-- ─────────────────────────────────────────────────────────────────────────────
-- Agrega columna descripcion a bitacoras_secciones y actualiza seed
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE dbo.bitacoras_secciones
    ADD descripcion NVARCHAR(300) NULL;
GO

UPDATE dbo.bitacoras_secciones SET descripcion = 'Control de pesajes, recepción y salida de grano, análisis de calidad' WHERE codigo = 'SEC-01';
UPDATE dbo.bitacoras_secciones SET descripcion = 'Descarga, evaluación visual del grano y monitoreo de condiciones'     WHERE codigo = 'SEC-02';
UPDATE dbo.bitacoras_secciones SET descripcion = 'Producción, clasificación y control de calidad del producto terminado' WHERE codigo = 'SEC-03';
UPDATE dbo.bitacoras_secciones SET descripcion = 'Descarga manual, almacenamiento y monitoreo de condiciones'            WHERE codigo = 'SEC-04';
UPDATE dbo.bitacoras_secciones SET descripcion = 'Verificación de unidades y control de producto despachado'             WHERE codigo = 'SEC-05';
UPDATE dbo.bitacoras_secciones SET descripcion = 'Bitácoras de inocuidad por definir'                                    WHERE codigo = 'SEC-06';
GO
