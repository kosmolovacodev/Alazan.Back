-- ============================================================
-- Tabla: inventario_silos
-- Propósito: Registro transaccional — una fila por recepción.
--   kg_bruto : se registra al asignar silo (volcado)
--   kg_neto  : se actualiza al guardar la preliquidación (bruto - tara)
-- Estatus: 'SinOC' (por defecto) | 'ConOC' (al vincular OC)
-- ============================================================

CREATE TABLE dbo.inventario_silos (
    id                  BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Relaciones
    volcado_id          BIGINT          NOT NULL,
    bascula_id          BIGINT          NOT NULL,
    bodega_id           BIGINT          NOT NULL,
    bodega_nombre       NVARCHAR(200)   NOT NULL,
    silo_numero         NVARCHAR(50)    NULL,

    -- Datos de la recepción
    ticket_numero       NVARCHAR(50)    NULL,
    kg_bruto            DECIMAL(18,2)   NOT NULL DEFAULT 0,   -- peso bruto del camión
    kg_neto             DECIMAL(18,2)   NOT NULL DEFAULT 0,   -- peso neto (bruto - tara), se llena en preliquidación
    toneladas_brutas    AS (kg_bruto / 1000.0) PERSISTED,
    toneladas_netas     AS (kg_neto  / 1000.0) PERSISTED,
    calibre             NVARCHAR(50)    NULL,
    grano_id            INT             NULL,

    -- Estado inventario
    status              NVARCHAR(20)    NOT NULL DEFAULT 'SinOC',  -- 'SinOC' | 'ConOC'
    oc_id               BIGINT          NULL,   -- FK a OC futura

    -- Auditoría
    sede_id             INT             NOT NULL,
    fecha_ingreso       DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    created_at          DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    updated_at          DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT UQ_inventario_silos_bascula UNIQUE (bascula_id)
);

CREATE INDEX IX_inventario_silos_bodega_sede
    ON dbo.inventario_silos (bodega_id, sede_id, status);

CREATE INDEX IX_inventario_silos_sede
    ON dbo.inventario_silos (sede_id, status);

CREATE INDEX IX_inventario_silos_fecha
    ON dbo.inventario_silos (sede_id, fecha_ingreso);
