-- Agrega columna logo_url a configuracion_sistema para almacenar el logo en base64 o URL
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.configuracion_sistema')
      AND name = 'logo_url'
)
BEGIN
    ALTER TABLE dbo.configuracion_sistema
    ADD logo_url NVARCHAR(MAX) NULL;
END
