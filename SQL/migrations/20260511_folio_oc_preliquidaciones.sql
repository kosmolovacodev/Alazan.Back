-- ============================================================
--  Tarea 4: Folio interno Alazan para Órdenes de Compra
--  Formato: OC-YYYY-NNNN  (secuencial por año y sede)
-- ============================================================

-- 1. Agregar columna
ALTER TABLE dbo.preliquidaciones
    ADD folio_oc VARCHAR(20) NULL;
GO

-- 2. Backfill: asignar folios a registros existentes
--    ordenados por created_at ASC para respetar el orden histórico
WITH numeradas AS (
    SELECT
        p.id,
        YEAR(p.created_at) AS anio,
        b.sede_id,
        ROW_NUMBER() OVER (
            PARTITION BY YEAR(p.created_at), b.sede_id
            ORDER BY p.created_at ASC, p.id ASC
        ) AS rn
    FROM dbo.preliquidaciones p
    INNER JOIN dbo.boletas b ON b.id = p.boleta_id
    WHERE p.folio_oc IS NULL
)
UPDATE p
SET p.folio_oc = 'OC-' + CAST(n.anio AS VARCHAR(4))
              + '-' + RIGHT('0000' + CAST(n.rn AS VARCHAR(4)), 4)
FROM dbo.preliquidaciones p
INNER JOIN numeradas n ON n.id = p.id;
GO
