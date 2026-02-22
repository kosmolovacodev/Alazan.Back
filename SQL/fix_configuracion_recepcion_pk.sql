-- =====================================================
-- Script: Corregir Configuracion_Recepcion_Reglas
-- Descripción: Asegura que sede_id sea UNIQUE y opcionalmente
--              convierte id a IDENTITY para mejor rendimiento
-- Fecha: 2026-02-04
-- =====================================================

USE [NombreDeTuBaseDeDatos]; -- Cambia esto por el nombre de tu BD
GO

PRINT '=====================================================';
PRINT 'DIAGNÓSTICO: Verificando estructura actual';
PRINT '=====================================================';
GO

-- Verificar si id es IDENTITY
DECLARE @isIdentity BIT;
SELECT @isIdentity = is_identity
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.Configuracion_Recepcion_Reglas')
AND name = 'id';

IF @isIdentity = 1
    PRINT '✓ La columna id YA es IDENTITY (auto-incremental)';
ELSE
    PRINT '✗ La columna id NO es IDENTITY (se calcula manualmente en código)';
GO

-- =====================================================
-- PASO 1: Crear constraint UNIQUE en sede_id (OBLIGATORIO)
-- =====================================================
-- Esto asegura que cada sede tenga solo una configuración

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Configuracion_Recepcion_Reglas_SedeId')
BEGIN
    PRINT '✓ Constraint UNIQUE en sede_id ya existe';
END
ELSE
BEGIN
    -- Verificar que no haya duplicados antes de crear el constraint
    DECLARE @duplicados INT;
    SELECT @duplicados = COUNT(*) - COUNT(DISTINCT sede_id)
    FROM dbo.Configuracion_Recepcion_Reglas;

    IF @duplicados > 0
    BEGIN
        PRINT '✗ ERROR: Hay configuraciones duplicadas para la misma sede';
        PRINT 'Debes limpiar los duplicados antes de crear el constraint UNIQUE';
        PRINT '';
        PRINT 'Ejecuta esto para ver los duplicados:';
        PRINT 'SELECT sede_id, COUNT(*) FROM dbo.Configuracion_Recepcion_Reglas GROUP BY sede_id HAVING COUNT(*) > 1';
    END
    ELSE
    BEGIN
        CREATE UNIQUE INDEX UQ_Configuracion_Recepcion_Reglas_SedeId
        ON dbo.Configuracion_Recepcion_Reglas(sede_id);

        PRINT '✓ Constraint UNIQUE creado en sede_id';
        PRINT '  Ahora cada sede puede tener solo una configuración';
    END
END
GO

-- =====================================================
-- PASO 2 (OPCIONAL): Convertir id a IDENTITY
-- =====================================================
-- NOTA: Este paso es OPCIONAL pero RECOMENDADO para mejor rendimiento
-- ADVERTENCIA: Requiere recrear la tabla, lo cual puede ser complejo
--              si hay relaciones con otras tablas

PRINT '';
PRINT '=====================================================';
PRINT 'RECOMENDACIÓN: Convertir id a IDENTITY';
PRINT '=====================================================';
PRINT 'Actualmente el código C# calcula manualmente el siguiente ID.';
PRINT 'Para mejor rendimiento y evitar race conditions, se recomienda';
PRINT 'convertir la columna id a IDENTITY.';
PRINT '';
PRINT 'Si deseas hacerlo, contacta a tu DBA para:';
PRINT '1. Respaldar la tabla';
PRINT '2. Recrear la tabla con id como IDENTITY(1,1)';
PRINT '3. Migrar los datos existentes';
PRINT '';
PRINT 'Mientras tanto, el código seguirá funcionando calculando el ID manualmente.';
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado';
PRINT '=====================================================';
GO
