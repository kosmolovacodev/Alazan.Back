-- ============================================================
-- Migración: Sección de Inicio de Día por Rol
-- Fecha: 2026-04-14
-- Descripción:
--   Agrega seccion_inicio_dia a dbo.roles para que cada rol
--   apunte explícitamente a una sección del Inicio de Día.
--   Valores: BASCULA | VOLCADO | PROCESO | ALMACEN | NULL
--   NULL = el rol no participa en Inicio de Día (ADMIN, TESORERO, etc.)
-- ============================================================

ALTER TABLE dbo.roles
    ADD seccion_inicio_dia NVARCHAR(20) NULL;
GO

-- Mapeo inicial con los roles existentes
UPDATE dbo.roles SET seccion_inicio_dia = 'BASCULA' WHERE nombre_rol IN ('SUPERVISOR_BASCULA', 'ANALISTA_BASCULA');
UPDATE dbo.roles SET seccion_inicio_dia = 'VOLCADO' WHERE nombre_rol IN ('SUPERVISOR_VOLCADO');
UPDATE dbo.roles SET seccion_inicio_dia = 'PROCESO' WHERE nombre_rol IN ('SUPERVISOR_PRODUCCION', 'ANALISTA_CALIDAD');
UPDATE dbo.roles SET seccion_inicio_dia = 'ALMACEN' WHERE nombre_rol IN ('SUPERVISOR_ALMACEN');
-- ADMIN, ENLACE_ADMINISTRATIVO, TESORERO, ENLACE_ADMINISTRATIVO → quedan NULL (no aplican)
GO
