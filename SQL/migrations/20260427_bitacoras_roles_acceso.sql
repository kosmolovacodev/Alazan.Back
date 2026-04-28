-- Agrega campo roles_acceso a bitacoras_secciones
-- NULL = visible para todos los roles
-- Valor = prefijos de rol separados por coma, ej: 'bascula,supervisor'
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.bitacoras_secciones') AND name = 'roles_acceso'
)
BEGIN
    ALTER TABLE dbo.bitacoras_secciones ADD roles_acceso NVARCHAR(500) NULL;
END
