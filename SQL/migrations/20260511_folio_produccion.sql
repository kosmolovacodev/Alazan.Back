-- ============================================================
--  Tarea 10: Folio autoincremental Órdenes de Producción
--  Formato: OP-YYYY-NNNN  (secuencial por año y sede)
-- ============================================================

-- 1. Agregar columna
ALTER TABLE dbo.ordenesproduccion
    ADD folio VARCHAR(20) NULL;
GO

-- 2. Backfill: asignar folios a registros existentes
--    ordenados por fecha_creacion ASC para respetar el orden histórico
WITH numeradas AS (
    SELECT
        p.id,
        YEAR(p.fecha_creacion) AS anio,
        p.sede_id,
        ROW_NUMBER() OVER (
            PARTITION BY YEAR(p.fecha_creacion), p.sede_id
            ORDER BY p.fecha_creacion ASC, p.id ASC
        ) AS rn
    FROM dbo.ordenesproduccion p
    WHERE p.folio IS NULL
)
UPDATE p
SET p.folio = 'OP-' + CAST(n.anio AS VARCHAR(4))
            + '-' + RIGHT('0000' + CAST(n.rn AS VARCHAR(4)), 4)
FROM dbo.ordenesproduccion p
INNER JOIN numeradas n ON n.id = p.id;
GO
