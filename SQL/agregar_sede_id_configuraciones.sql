-- =====================================================
-- Script: Agregar sede_id a tablas de configuración
-- Descripción: Agrega la columna sede_id a todas las
--              tablas de configuración para soporte multisede
-- Fecha: 2026-02-03
-- =====================================================

USE [NombreDeTuBaseDeDatos]; -- Cambia esto por el nombre de tu BD
GO

-- =====================================================
-- 1. CONFIGURACIÓN DE RECEPCIÓN
-- =====================================================

-- Tabla: Configuracion_Recepcion_Reglas
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Recepcion_Reglas') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Recepcion_Reglas
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Recepcion_Reglas';
END
ELSE
    PRINT 'Configuracion_Recepcion_Reglas ya tiene sede_id';
GO

-- Tabla: Configuracion_Campos_Pantallas
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Campos_Pantallas') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Campos_Pantallas
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Campos_Pantallas';
END
ELSE
    PRINT 'Configuracion_Campos_Pantallas ya tiene sede_id';
GO

-- =====================================================
-- 2. CONFIGURACIÓN DE FACTURACIÓN
-- =====================================================

-- Tabla: Configuracion_Facturacion_General
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_General') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_General
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_General';
END
ELSE
    PRINT 'Configuracion_Facturacion_General ya tiene sede_id';
GO

-- Tabla: Configuracion_Facturacion_Documentos
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_Documentos') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_Documentos
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_Documentos';
END
ELSE
    PRINT 'Configuracion_Facturacion_Documentos ya tiene sede_id';
GO

-- Tabla: Configuracion_Facturacion_Status
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_Status') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_Status
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_Status';
END
ELSE
    PRINT 'Configuracion_Facturacion_Status ya tiene sede_id';
GO

-- Tabla: Configuracion_Facturacion_Endoso
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_Endoso') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_Endoso
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_Endoso';
END
ELSE
    PRINT 'Configuracion_Facturacion_Endoso ya tiene sede_id';
GO

-- Tabla: Configuracion_Facturacion_Endoso_Clausulas
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_Endoso_Clausulas') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_Endoso_Clausulas
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_Endoso_Clausulas';
END
ELSE
    PRINT 'Configuracion_Facturacion_Endoso_Clausulas ya tiene sede_id';
GO

-- Tabla: Configuracion_Facturacion_Endoso_Docs
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Facturacion_Endoso_Docs') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Facturacion_Endoso_Docs
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Facturacion_Endoso_Docs';
END
ELSE
    PRINT 'Configuracion_Facturacion_Endoso_Docs ya tiene sede_id';
GO

-- =====================================================
-- 3. CONFIGURACIÓN DE PAGOS
-- =====================================================

-- Tabla: Configuracion_Pagos_General
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Pagos_General') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Pagos_General
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Pagos_General';
END
ELSE
    PRINT 'Configuracion_Pagos_General ya tiene sede_id';
GO

-- Tabla: Configuracion_Pagos_Status
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Pagos_Status') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Pagos_Status
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Pagos_Status';
END
ELSE
    PRINT 'Configuracion_Pagos_Status ya tiene sede_id';
GO

-- Tabla: Configuracion_Pagos_Formas
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Pagos_Formas') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Pagos_Formas
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Pagos_Formas';
END
ELSE
    PRINT 'Configuracion_Pagos_Formas ya tiene sede_id';
GO

-- Tabla: Configuracion_Pagos_Dias
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Configuracion_Pagos_Dias') AND name = 'sede_id')
BEGIN
    ALTER TABLE dbo.Configuracion_Pagos_Dias
    ADD sede_id INT NOT NULL DEFAULT 0;

    PRINT 'Columna sede_id agregada a Configuracion_Pagos_Dias';
END
ELSE
    PRINT 'Configuracion_Pagos_Dias ya tiene sede_id';
GO

-- =====================================================
-- 4. CREAR ÍNDICES PARA OPTIMIZAR CONSULTAS
-- =====================================================

-- Índice para Configuracion_Recepcion_Reglas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Configuracion_Recepcion_Reglas_SedeId')
BEGIN
    CREATE INDEX IX_Configuracion_Recepcion_Reglas_SedeId
    ON dbo.Configuracion_Recepcion_Reglas(sede_id);
    PRINT 'Índice creado en Configuracion_Recepcion_Reglas';
END
GO

-- Índice para Configuracion_Campos_Pantallas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Configuracion_Campos_Pantallas_SedeId')
BEGIN
    CREATE INDEX IX_Configuracion_Campos_Pantallas_SedeId
    ON dbo.Configuracion_Campos_Pantallas(sede_id);
    PRINT 'Índice creado en Configuracion_Campos_Pantallas';
END
GO

-- Índice para Configuracion_Facturacion_General
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Configuracion_Facturacion_General_SedeId')
BEGIN
    CREATE INDEX IX_Configuracion_Facturacion_General_SedeId
    ON dbo.Configuracion_Facturacion_General(sede_id);
    PRINT 'Índice creado en Configuracion_Facturacion_General';
END
GO

-- Índice para Configuracion_Pagos_General
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Configuracion_Pagos_General_SedeId')
BEGIN
    CREATE INDEX IX_Configuracion_Pagos_General_SedeId
    ON dbo.Configuracion_Pagos_General(sede_id);
    PRINT 'Índice creado en Configuracion_Pagos_General';
END
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado exitosamente';
PRINT 'Todas las tablas de configuración ahora tienen sede_id';
PRINT '=====================================================';
GO
