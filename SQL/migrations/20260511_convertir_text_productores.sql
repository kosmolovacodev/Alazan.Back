-- ============================================================
--  Fix: convierte todas las columnas TEXT de dbo.productores
--  a NVARCHAR. El tipo TEXT no permite comparaciones con =
--  ni funciona en MERGE ON, WHERE x='y', etc.
--  Usa EXEC sp_executesql en cada paso para evitar el error
--  "Invalid column name" al agregar y usar en el mismo batch.
-- ============================================================

DECLARE @col    NVARCHAR(128);
DECLARE @sql    NVARCHAR(MAX);
DECLARE @rename NVARCHAR(256);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT name
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.productores')
      AND system_type_id = TYPE_ID(N'text')
    ORDER BY column_id;

OPEN cur;
FETCH NEXT FROM cur INTO @col;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 1. Agregar columna temporal NVARCHAR(MAX)
    SET @sql = N'ALTER TABLE dbo.productores ADD '
             + QUOTENAME(@col + N'_nv')
             + N' NVARCHAR(MAX) NULL;';
    EXEC sp_executesql @sql;

    -- 2. Copiar datos (EXEC obliga recompilación, ya ve la columna nueva)
    SET @sql = N'UPDATE dbo.productores SET '
             + QUOTENAME(@col + N'_nv')
             + N' = CAST(' + QUOTENAME(@col) + N' AS NVARCHAR(MAX));';
    EXEC sp_executesql @sql;

    -- 3. Eliminar columna TEXT original
    SET @sql = N'ALTER TABLE dbo.productores DROP COLUMN '
             + QUOTENAME(@col) + N';';
    EXEC sp_executesql @sql;

    -- 4. Renombrar columna temporal al nombre original
    SET @rename = N'dbo.productores.' + @col + N'_nv';
    EXEC sp_rename @rename, @col, N'COLUMN';

    PRINT 'Convertida: ' + @col;

    FETCH NEXT FROM cur INTO @col;
END

CLOSE cur;
DEALLOCATE cur;
GO

-- ── Ajuste de longitudes para columnas clave ─────────────────
-- NVARCHAR(MAX) funciona, pero columnas cortas son más eficientes
-- en índices y comparaciones. Ajustamos solo las que usamos en WHERE/JOIN.

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.productores')
      AND name = N'rfc' AND max_length = -1  -- -1 = MAX
)
    ALTER TABLE dbo.productores ALTER COLUMN rfc NVARCHAR(50) NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.productores')
      AND name = N'origen' AND max_length = -1
)
    ALTER TABLE dbo.productores ALTER COLUMN origen NVARCHAR(50) NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.productores')
      AND name = N'pais' AND max_length = -1
)
    ALTER TABLE dbo.productores ALTER COLUMN pais NVARCHAR(100) NULL;

PRINT 'Longitudes ajustadas. Migración completa.';
GO
