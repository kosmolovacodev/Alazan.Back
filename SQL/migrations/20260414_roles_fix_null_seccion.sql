-- ============================================================
-- Parche: actualizar roles creados manualmente desde la UI
-- que quedaron con seccion_inicio_dia / tipo_inicio_dia en NULL
-- porque el POST original no los guardaba.
-- Ajusta los valores según el nombre del rol.
-- ============================================================

-- Supervisores
UPDATE dbo.roles
SET seccion_inicio_dia = 'BASCULA', tipo_inicio_dia = 'SUPERVISOR'
WHERE nombre_rol LIKE '%SUPERVISOR%BASCULA%'  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'VOLCADO', tipo_inicio_dia = 'SUPERVISOR'
WHERE nombre_rol LIKE '%SUPERVISOR%VOLCADO%'  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'PROCESO', tipo_inicio_dia = 'SUPERVISOR'
WHERE (nombre_rol LIKE '%SUPERVISOR%PRODUCCION%' OR nombre_rol LIKE '%SUPERVISOR%PROCESO%')
  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'ALMACEN', tipo_inicio_dia = 'SUPERVISOR'
WHERE nombre_rol LIKE '%SUPERVISOR%ALMACEN%'  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

-- Asistentes / Analistas
UPDATE dbo.roles
SET seccion_inicio_dia = 'BASCULA', tipo_inicio_dia = 'ASISTENTE'
WHERE (nombre_rol LIKE '%ASISTENTE%BASCULA%' OR nombre_rol LIKE '%ANALISTA%BASCULA%')
  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'VOLCADO', tipo_inicio_dia = 'ASISTENTE'
WHERE (nombre_rol LIKE '%ASISTENTE%VOLCADO%' OR nombre_rol LIKE '%ANALISTA%VOLCADO%')
  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'PROCESO', tipo_inicio_dia = 'ASISTENTE'
WHERE (nombre_rol LIKE '%ASISTENTE%PROCESO%' OR nombre_rol LIKE '%ASISTENTE%PRODUCCION%'
    OR nombre_rol LIKE '%ANALISTA%CALIDAD%')
  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);

UPDATE dbo.roles
SET seccion_inicio_dia = 'ALMACEN', tipo_inicio_dia = 'ASISTENTE'
WHERE (nombre_rol LIKE '%ASISTENTE%ALMACEN%' OR nombre_rol LIKE '%ANALISTA%ALMACEN%')
  AND (seccion_inicio_dia IS NULL OR tipo_inicio_dia IS NULL);
GO
