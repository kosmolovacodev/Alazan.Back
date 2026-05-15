-- ============================================================
--  Fix: columna 'pais' demasiado corta (TEXT o NVARCHAR(10))
--  para valores como "ESTADOS UNIDOS" (14 chars).
-- ============================================================
DECLARE @tipo NVARCHAR(50) = (
    SELECT TYPE_NAME(system_type_id)
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.productores')
      AND name = N'pais'
);

IF @tipo IS NULL
    PRINT 'Columna pais no encontrada — nada que hacer.';

ELSE IF @tipo IN (N'nvarchar', N'varchar', N'nchar', N'char')
BEGIN
    ALTER TABLE dbo.productores ALTER COLUMN pais NVARCHAR(100) NULL;
    PRINT 'Columna pais expandida a NVARCHAR(100).';
END

ELSE IF @tipo = N'text'
BEGIN
    -- text no puede alterarse directamente: agregar columna nueva, copiar, renombrar
    ALTER TABLE dbo.productores ADD pais_nv NVARCHAR(100) NULL;
    UPDATE dbo.productores SET pais_nv = CAST(pais AS NVARCHAR(100));
    ALTER TABLE dbo.productores DROP COLUMN pais;
    EXEC sp_rename 'dbo.productores.pais_nv', 'pais', 'COLUMN';
    PRINT 'Columna pais (text) recreada como NVARCHAR(100).';
END
GO
