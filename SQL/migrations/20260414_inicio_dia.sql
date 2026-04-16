-- ============================================================
-- Migración: Inicio de Día
-- Fecha: 2026-04-14
-- Descripción:
--   Registra la asistencia diaria del personal por sede.
--   Las secciones (BASCULA, VOLCADO, PROCESO, ALMACEN) se derivan
--   del campo departamento del usuario o del nombre de su rol.
--   No se necesita tabla de puestos: los roles existentes ya los definen.
-- ============================================================

-- ─────────────────────────────────────────────────────────────
-- 1. Asistencias diarias de personal
-- ─────────────────────────────────────────────────────────────
CREATE TABLE dbo.inicio_dia_asistencias (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    sede_id         INT            NOT NULL,
    fecha           DATE           NOT NULL,
    usuario_id      BIGINT         NOT NULL,
    asistio         BIT            NOT NULL DEFAULT 1,
    registrado_por  BIGINT         NOT NULL,
    created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_inicio_dia_asistencias UNIQUE (sede_id, fecha, usuario_id)
);

-- ─────────────────────────────────────────────────────────────
-- 2. Marcador de día iniciado (lo cierra el Admin/Gerente)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE dbo.inicio_dia_dias (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    sede_id     INT            NOT NULL,
    fecha       DATE           NOT NULL,
    completo    BIT            NOT NULL DEFAULT 0,
    iniciado_por BIGINT        NULL,   -- quién abrió el día (supervisor, admin o asistente)
    created_at  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_inicio_dia_dias UNIQUE (sede_id, fecha)
);
GO
