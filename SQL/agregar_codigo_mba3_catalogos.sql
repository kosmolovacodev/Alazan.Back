-- ============================================================
-- Migración: Agregar campo 'codigo' para integración MBA3
-- Afecta: sedes_catalogo, silos_calibre_catalogo,
--         silos_pulmon_catalogo, catalogo_almacenes
--
-- 'codigo' = código corto que MBA3 usa para identificar:
--   • sedes_catalogo       → sucursal_origen  (Ej: GML para Guamuchil)
--   • silos_calibre_catalogo → codigo_bodega  (SM1, SM2, SN1, SN2, SV1, SV2)
--   • silos_pulmon_catalogo  → codigo_bodega  (SO3 - Silo Tolva único)
--   • catalogo_almacenes     → codigo_bodega  (BM1-BM6 - Bodegas de frijol)
-- ============================================================

-- 1. Sedes (Bodegas/Sucursales) ─ usado como sucursal_origen
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME   = 'sedes_catalogo'
      AND COLUMN_NAME  = 'codigo'
)
BEGIN
    ALTER TABLE dbo.sedes_catalogo ADD codigo NVARCHAR(20) NULL;
    PRINT 'Columna codigo agregada a sedes_catalogo';
END
ELSE
    PRINT 'sedes_catalogo.codigo ya existe, omitiendo.';

-- 2. Silos Calibre ─ usado como codigo_bodega
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME   = 'silos_calibre_catalogo'
      AND COLUMN_NAME  = 'codigo'
)
BEGIN
    ALTER TABLE dbo.silos_calibre_catalogo ADD codigo NVARCHAR(20) NULL;
    PRINT 'Columna codigo agregada a silos_calibre_catalogo';
END
ELSE
    PRINT 'silos_calibre_catalogo.codigo ya existe, omitiendo.';

-- 3. Silos Pulmón ─ usado como codigo_bodega
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME   = 'silos_pulmon_catalogo'
      AND COLUMN_NAME  = 'codigo'
)
BEGIN
    ALTER TABLE dbo.silos_pulmon_catalogo ADD codigo NVARCHAR(20) NULL;
    PRINT 'Columna codigo agregada a silos_pulmon_catalogo';
END
ELSE
    PRINT 'silos_pulmon_catalogo.codigo ya existe, omitiendo.';

-- 4. Almacenes (Bodegas de frijol BM1-BM6) ─ usado como codigo_bodega
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME   = 'catalogo_almacenes'
      AND COLUMN_NAME  = 'codigo'
)
BEGIN
    ALTER TABLE dbo.catalogo_almacenes ADD codigo NVARCHAR(20) NULL;
    PRINT 'Columna codigo agregada a catalogo_almacenes';
END
ELSE
    PRINT 'catalogo_almacenes.codigo ya existe, omitiendo.';
