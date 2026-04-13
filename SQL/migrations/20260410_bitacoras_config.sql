-- ============================================================
-- Migración: Configuración por bitácora (firmas, periodicidad)
-- Fecha: 2026-04-10
-- ============================================================

CREATE TABLE dbo.bitacoras_config (
    id                INT IDENTITY(1,1) PRIMARY KEY,
    codigo_bitacora   NVARCHAR(20) NOT NULL,          -- FO-HC-IMP-002
    periodicidad      NVARCHAR(20) NOT NULL DEFAULT 'Diaria',
        -- Diaria | Semanal | Quincenal | Mensual
    rol_operativo     BIT NOT NULL DEFAULT 0,
    firmas_operativo  INT NOT NULL DEFAULT 2,
    rol_supervisor    BIT NOT NULL DEFAULT 0,
    firmas_supervisor INT NOT NULL DEFAULT 1,
    rol_gerente       BIT NOT NULL DEFAULT 0,
    firmas_gerente    INT NOT NULL DEFAULT 1,
    firma_recepcion   BIT NOT NULL DEFAULT 0,
    updated_at        DATETIME NULL
);

CREATE UNIQUE INDEX UQ_bitacoras_config ON dbo.bitacoras_config (codigo_bitacora);
