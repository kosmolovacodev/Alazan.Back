-- =====================================================
-- Script: Insertar datos de ejemplo en Configuracion_Precio
-- Descripción: Inserta precios base de ejemplo para diferentes
--              granos y calibres
-- Fecha: 2026-02-04
-- =====================================================

USE [NombreDeTuBaseDeDatos]; -- Cambia esto por el nombre de tu BD
GO

PRINT 'Insertando precios de ejemplo en Configuracion_Precio...';
GO

-- Eliminar datos de ejemplo anteriores si existen
DELETE FROM dbo.Configuracion_Precio WHERE codigo_precio LIKE '%EJEMPLO%';
GO

-- Insertar precios de ejemplo para CACAHUATE
-- Asegúrate de cambiar:
--   - sede_id: al ID de tu sede
--   - grano_id: al ID del grano cacahuate en tu tabla 'granos'

-- Ejemplo 1: Cacahuate calibre 44-46
INSERT INTO dbo.Configuracion_Precio (
    sede_id,
    grano_id,
    calibre,
    precio_base_ton,
    codigo_precio,
    penalizacion_por_punto_pct,
    activo,
    habilitar_autorizacion_automatica,
    minutos_para_autorizacion,
    tolerancia_precio_pct,
    requiere_autorizacion_fuera_tolerancia,
    created_at,
    updated_at
) VALUES (
    8,                      -- sede_id (cambia según tu sede)
    4,                      -- grano_id (busca el ID de cacahuate en tu tabla granos)
    '44-46',                -- calibre
    5500.00,                -- precio_base_ton (MXN por tonelada)
    'CACAHUATE-44-46-EJEMPLO', -- codigo_precio
    2.5,                    -- penalizacion: reduce 2.5% del precio por cada punto de descuento
    1,                      -- activo
    0,                      -- habilitar_autorizacion_automatica
    30,                     -- minutos_para_autorizacion
    5.0,                    -- tolerancia_precio_pct (5%)
    1,                      -- requiere_autorizacion_fuera_tolerancia
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);

-- Ejemplo 2: Cacahuate calibre 48-50 (mayor calibre, mayor precio)
INSERT INTO dbo.Configuracion_Precio (
    sede_id,
    grano_id,
    calibre,
    precio_base_ton,
    codigo_precio,
    penalizacion_por_punto_pct,
    activo,
    habilitar_autorizacion_automatica,
    minutos_para_autorizacion,
    tolerancia_precio_pct,
    requiere_autorizacion_fuera_tolerancia,
    created_at,
    updated_at
) VALUES (
    8,                      -- sede_id
    4,                      -- grano_id
    '48-50',                -- calibre
    5800.00,                -- precio_base_ton (precio mayor para calibre mayor)
    'CACAHUATE-48-50-EJEMPLO',
    2.5,
    1,
    0,
    30,
    5.0,
    1,
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);

-- Ejemplo 3: Cacahuate calibre 40-42 (menor calibre, menor precio)
INSERT INTO dbo.Configuracion_Precio (
    sede_id,
    grano_id,
    calibre,
    precio_base_ton,
    codigo_precio,
    penalizacion_por_punto_pct,
    activo,
    habilitar_autorizacion_automatica,
    minutos_para_autorizacion,
    tolerancia_precio_pct,
    requiere_autorizacion_fuera_tolerancia,
    created_at,
    updated_at
) VALUES (
    8,
    4,
    '40-42',
    5200.00,
    'CACAHUATE-40-42-EJEMPLO',
    2.5,
    1,
    0,
    30,
    5.0,
    1,
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);

PRINT '✓ Precios de ejemplo insertados';
GO

-- Verificar los datos insertados
SELECT
    id,
    sede_id,
    grano_id,
    calibre,
    precio_base_ton,
    codigo_precio,
    penalizacion_por_punto_pct,
    activo
FROM dbo.Configuracion_Precio
WHERE codigo_precio LIKE '%EJEMPLO%';
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado exitosamente';
PRINT '';
PRINT 'NOTA IMPORTANTE:';
PRINT '- Verifica que los IDs de sede_id y grano_id sean correctos';
PRINT '- Ajusta los precios según tu negocio';
PRINT '- Puedes agregar más calibres según necesites';
PRINT '- Para usar estos precios en producción, cambia EJEMPLO por un código apropiado';
PRINT '=====================================================';
GO
