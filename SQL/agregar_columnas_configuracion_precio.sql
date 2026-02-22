-- =====================================================
-- Script: Agregar columnas faltantes a Configuracion_Precio
-- Descripción: Añade las columnas necesarias para el manejo
--              de precios por grano y calibre
-- Fecha: 2026-02-04
-- =====================================================

USE [NombreDeTuBaseDeDatos]; -- Cambia esto por el nombre de tu BD
GO

PRINT 'Agregando columnas faltantes a Configuracion_Precio...';
GO

-- Agregar columna grano_id
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'grano_id')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [grano_id] INT NULL;
    PRINT '✓ Columna grano_id agregada';
END
ELSE
    PRINT '✓ Columna grano_id ya existe';
GO

-- Agregar columna calibre
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'calibre')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [calibre] NVARCHAR(50) NULL;
    PRINT '✓ Columna calibre agregada';
END
ELSE
    PRINT '✓ Columna calibre ya existe';
GO

-- Agregar columna precio_base_ton
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'precio_base_ton')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [precio_base_ton] DECIMAL(18,2) NULL;
    PRINT '✓ Columna precio_base_ton agregada';
END
ELSE
    PRINT '✓ Columna precio_base_ton ya existe';
GO

-- Agregar columna codigo_precio
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'codigo_precio')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [codigo_precio] NVARCHAR(50) NULL;
    PRINT '✓ Columna codigo_precio agregada';
END
ELSE
    PRINT '✓ Columna codigo_precio ya existe';
GO

-- Agregar columna penalizacion_por_punto_pct
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'penalizacion_por_punto_pct')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [penalizacion_por_punto_pct] DECIMAL(5,2) NULL DEFAULT 0;
    PRINT '✓ Columna penalizacion_por_punto_pct agregada';
END
ELSE
    PRINT '✓ Columna penalizacion_por_punto_pct ya existe';
GO

-- Agregar columna activo
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'activo')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [activo] BIT NULL DEFAULT 1;
    PRINT '✓ Columna activo agregada';
END
ELSE
    PRINT '✓ Columna activo ya existe';
GO

-- Agregar columnas de auditoría si no existen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'created_at')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [created_at] DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET();
    PRINT '✓ Columna created_at agregada';
END
ELSE
    PRINT '✓ Columna created_at ya existe';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND name = 'updated_at')
BEGIN
    ALTER TABLE [dbo].[Configuracion_Precio]
    ADD [updated_at] DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET();
    PRINT '✓ Columna updated_at agregada';
END
ELSE
    PRINT '✓ Columna updated_at ya existe';
GO

-- Crear índice UNIQUE en (sede_id, grano_id, calibre) si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Configuracion_Precio_Sede_Grano_Calibre')
BEGIN
    CREATE UNIQUE INDEX UQ_Configuracion_Precio_Sede_Grano_Calibre
    ON dbo.Configuracion_Precio(sede_id, grano_id, calibre)
    WHERE activo = 1 AND grano_id IS NOT NULL AND calibre IS NOT NULL;
    PRINT '✓ Índice UQ_Configuracion_Precio_Sede_Grano_Calibre creado';
END
ELSE
    PRINT '✓ Índice UQ_Configuracion_Precio_Sede_Grano_Calibre ya existe';
GO

-- Actualizar valores NULL de activo a 1 (activo por defecto)
UPDATE [dbo].[Configuracion_Precio]
SET [activo] = 1
WHERE [activo] IS NULL;
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado exitosamente';
PRINT 'Columnas agregadas a Configuracion_Precio';
PRINT '';
PRINT 'IMPORTANTE: Ahora debes insertar precios base para tus granos y calibres';
PRINT 'Ejemplo:';
PRINT 'INSERT INTO dbo.Configuracion_Precio (sede_id, grano_id, calibre, precio_base_ton, codigo_precio, penalizacion_por_punto_pct, activo)';
PRINT 'VALUES (1, 4, ''44-46'', 5500.00, ''CACAHUATE-44-46'', 2.5, 1);';
PRINT '=====================================================';
GO
