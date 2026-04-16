-- ============================================================
-- Migración: Tipo de participación en Inicio de Día por Rol
-- Fecha: 2026-04-14
-- Nota: seccion_inicio_dia ya fue agregado en la migración anterior.
--       Este script solo agrega tipo_inicio_dia (con guard IF NOT EXISTS).
--
--   SUPERVISOR → llena asistencia de su sección directamente
--   ASISTENTE  → pregunta "¿asistió el supervisor?" y si no,
--                selecciona quién cubrirá como analista
--   OPERADOR   → pantalla de espera ("contacte a su supervisor")
--   NULL       → no participa (ADMIN, TESORERO, etc.)
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.roles') AND name = 'tipo_inicio_dia'
)
    ALTER TABLE dbo.roles ADD tipo_inicio_dia NVARCHAR(20) NULL;
GO

-- Mapeo con roles existentes
UPDATE dbo.roles SET tipo_inicio_dia = 'SUPERVISOR' WHERE nombre_rol IN (
    'SUPERVISOR_BASCULA', 'SUPERVISOR_VOLCADO', 'SUPERVISOR_PRODUCCION', 'SUPERVISOR_ALMACEN'
);
UPDATE dbo.roles SET tipo_inicio_dia = 'ASISTENTE' WHERE nombre_rol IN ('ANALISTA_BASCULA');
UPDATE dbo.roles SET tipo_inicio_dia = 'OPERADOR'  WHERE nombre_rol IN ('ANALISTA_CALIDAD', 'ENLACE_ADMINISTRATIVO');
GO

-- ─────────────────────────────────────────────────────────────
-- Renombrar cerrado_por → iniciado_por en inicio_dia_dias
-- (semánticamente es quien ABRE el día, no quien lo cierra)
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.inicio_dia_dias') AND name = 'cerrado_por'
)
    EXEC sp_rename 'dbo.inicio_dia_dias.cerrado_por', 'iniciado_por', 'COLUMN';
GO
