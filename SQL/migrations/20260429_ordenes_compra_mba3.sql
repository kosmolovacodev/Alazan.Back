-- ============================================================
-- Módulo: Órdenes de Compra MBA3
-- Tablas: mba3_ordenes_compra  +  mba3_ordenes_compra_historial
-- ============================================================

CREATE TABLE dbo.mba3_ordenes_compra (
    id                  INT IDENTITY(1,1)   NOT NULL,

    -- Identificadores únicos MBA3
    contrato_id         BIGINT              NOT NULL,
    contrato_id_corp    NVARCHAR(60)        NOT NULL,   -- "34417-BGAR1-OC"
    pk_uuid             NVARCHAR(50)        NULL,
    corp                NVARCHAR(20)        NULL,

    -- Cliente en MBA3
    client_id           NVARCHAR(20)        NULL,
    client_id_corp      NVARCHAR(60)        NULL,
    salesman            NVARCHAR(20)        NULL,

    -- Fechas
    fecha_pedido        DATE                NULL,
    fecha_desde         DATE                NULL,
    fecha_hasta         DATE                NULL,
    fecha_entrega       DATE                NULL,

    -- Financiero
    inv_amount          DECIMAL(18,4)       NULL,
    currency_type       NVARCHAR(10)        NULL,
    descuento_porc      DECIMAL(10,4)       NULL,

    -- Estado crudo de MBA3 (se audita en historial)
    status              NVARCHAR(50)        NULL,
    confirmed           BIT                 NULL,
    tipo_doc_oc_pe_ct   NVARCHAR(10)        NULL,
    tipo_orden_compra   NVARCHAR(10)        NULL,
    ware_code           NVARCHAR(20)        NULL,

    -- Referencia y proveedor embebido en el OC
    referencia_general  NVARCHAR(100)       NULL,
    pais_proveedor      NVARCHAR(100)       NULL,
    ciudad_proveedor    NVARCHAR(100)       NULL,

    -- Vínculo al productor local (resuelto por match de nombre)
    productor_id        BIGINT              NULL
        REFERENCES dbo.productores(id),

    -- JSON con el payload completo de MBA3 (todos los ~145 campos)
    datos_json          NVARCHAR(MAX)       NULL,

    -- Auditoría de sincronización
    primera_sync        DATETIME2           NOT NULL DEFAULT GETDATE(),
    ultima_sync         DATETIME2           NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_mba3_ordenes_compra        PRIMARY KEY (id),
    CONSTRAINT UQ_mba3_ordenes_compra_corp   UNIQUE (contrato_id_corp)
);

CREATE INDEX IX_mba3_oc_productor  ON dbo.mba3_ordenes_compra (productor_id);
CREATE INDEX IX_mba3_oc_status     ON dbo.mba3_ordenes_compra (status);
CREATE INDEX IX_mba3_oc_fecha      ON dbo.mba3_ordenes_compra (fecha_pedido);
CREATE INDEX IX_mba3_oc_client     ON dbo.mba3_ordenes_compra (client_id);

-- ============================================================
-- Historial de cambios de STATUS (una fila por cambio detectado)
-- ============================================================

CREATE TABLE dbo.mba3_ordenes_compra_historial (
    id              INT IDENTITY(1,1)   NOT NULL,
    orden_id        INT                 NOT NULL
        REFERENCES dbo.mba3_ordenes_compra(id),
    status_anterior NVARCHAR(50)        NULL,
    status_nuevo    NVARCHAR(50)        NULL,
    fecha_cambio    DATETIME2           NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_mba3_oc_historial PRIMARY KEY (id)
);

CREATE INDEX IX_mba3_oc_hist_orden ON dbo.mba3_ordenes_compra_historial (orden_id);
