-- ============================================================
-- Migración: Módulo Bitácoras Operativas
-- Fecha: 2026-04-10
-- ============================================================

CREATE TABLE dbo.bitacoras_registros (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    codigo_bitacora NVARCHAR(20)  NOT NULL,    -- FO-HC-IMP-002
    seccion_codigo  NVARCHAR(10)  NOT NULL,    -- SEC-01
    fecha           DATE          NOT NULL DEFAULT GETDATE(),
    status          NVARCHAR(20)  NOT NULL DEFAULT 'Pendiente',
        -- Pendiente | Firmada | Impresa
    datos_json      NVARCHAR(MAX) NULL,        -- JSON con los valores de cada columna
    sede_id         INT           NOT NULL DEFAULT 0,
    activo          BIT           NOT NULL DEFAULT 1,
    created_at      DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE INDEX IX_bitacoras_codigo ON dbo.bitacoras_registros (codigo_bitacora, sede_id);
CREATE INDEX IX_bitacoras_seccion ON dbo.bitacoras_registros (seccion_codigo, sede_id);
