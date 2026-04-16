-- ============================================================
-- Migración: agregar columna seccion a inicio_dia_dias
-- Permite registrar por separado cuándo y quién inició
-- cada sección (BASCULA, VOLCADO, PROCESO, ALMACEN).
-- ============================================================

-- 1. Quitar la restricción UNIQUE actual (sede_id, fecha)
ALTER TABLE dbo.inicio_dia_dias DROP CONSTRAINT UQ_inicio_dia_dias;

-- 2. Agregar columna seccion
ALTER TABLE dbo.inicio_dia_dias ADD seccion NVARCHAR(20) NULL;

-- 3. Índices únicos parciales:
--    - uno para registros CON sección (BASCULA, VOLCADO, etc.)
--    - uno para registros SIN sección (inicio global por ADMIN)
CREATE UNIQUE INDEX UX_inicio_dia_dias_seccion
    ON dbo.inicio_dia_dias (sede_id, fecha, seccion)
    WHERE seccion IS NOT NULL;

CREATE UNIQUE INDEX UX_inicio_dia_dias_global
    ON dbo.inicio_dia_dias (sede_id, fecha)
    WHERE seccion IS NULL;

GO
