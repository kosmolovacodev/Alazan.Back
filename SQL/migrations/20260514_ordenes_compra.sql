-- ============================================================
-- Tarea 4: Tabla propia para Órdenes de Compra (Alazan interno)
-- Folio propio: OC-YYYY-NNNN  (secuencial por año y sede)
-- Campo folio_mba3: vincula con mba3_ordenes_compra.contrato_id_corp
-- ============================================================

CREATE TABLE dbo.ordenes_compra (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    folio               VARCHAR(20)     NOT NULL,           -- OC-2026-0001
    sede_id             INT             NOT NULL,
    preliquidacion_id   BIGINT          NULL,               -- sin FK constraint: preliquidaciones.id es BIGINT
    folio_mba3          NVARCHAR(60)    NULL,               -- contrato_id_corp de mba3_ordenes_compra
    productor_id        BIGINT          NULL
        REFERENCES dbo.productores(id),
    status              NVARCHAR(30)    NOT NULL DEFAULT 'Generada',
    fecha               DATE            NOT NULL DEFAULT GETDATE(),
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at          DATETIME        NULL,
    activo              BIT             NOT NULL DEFAULT 1,
    CONSTRAINT UQ_ordenes_compra_folio UNIQUE (folio)
);

CREATE INDEX IX_oc_sede       ON dbo.ordenes_compra (sede_id);
CREATE INDEX IX_oc_preliq     ON dbo.ordenes_compra (preliquidacion_id);
CREATE INDEX IX_oc_mba3       ON dbo.ordenes_compra (folio_mba3);
CREATE INDEX IX_oc_productor  ON dbo.ordenes_compra (productor_id);
GO

-- Backfill: migrar folios existentes de preliquidaciones → ordenes_compra
-- preliquidaciones ya tiene sede_id y productor_id directos, no se necesita join
INSERT INTO dbo.ordenes_compra
    (folio, sede_id, preliquidacion_id, productor_id, fecha, created_at)
SELECT
    p.folio_oc,
    p.sede_id,
    p.id,
    p.productor_id,
    CAST(p.created_at AS DATE),
    p.created_at
FROM dbo.preliquidaciones p
WHERE p.folio_oc IS NOT NULL;
GO
