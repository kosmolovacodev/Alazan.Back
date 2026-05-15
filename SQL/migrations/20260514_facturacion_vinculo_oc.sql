-- ============================================================
-- Vincular facturacion_recepciones con ordenes_compra
-- ============================================================

ALTER TABLE dbo.facturacion_recepciones
    ADD ordenes_compra_id INT NULL
        REFERENCES dbo.ordenes_compra(id);

CREATE INDEX IX_facturacion_oc ON dbo.facturacion_recepciones (ordenes_compra_id);
GO
