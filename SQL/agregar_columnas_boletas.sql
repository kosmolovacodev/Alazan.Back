-- =====================================================
-- Script: Agregar columnas faltantes a tabla boletas
-- Descripción: Añade las columnas necesarias para el
--              registro completo de boletas de análisis
-- Fecha: 2026-02-04
-- =====================================================

USE [Alazan]; -- Cambia esto por el nombre de tu BD
GO

PRINT 'Agregando columnas faltantes a tabla boletas...';
GO

-- Agregar columna sede_id
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'sede_id')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [sede_id] INT NULL;
    PRINT '✓ Columna sede_id agregada';
END
ELSE
    PRINT '✓ Columna sede_id ya existe';
GO

-- Agregar columna folio
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'folio')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [folio] NVARCHAR(50) NULL;
    PRINT '✓ Columna folio agregada';
END
ELSE
    PRINT '✓ Columna folio ya existe';
GO

-- Agregar columna ticket_numero
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'ticket_numero')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [ticket_numero] NVARCHAR(50) NULL;
    PRINT '✓ Columna ticket_numero agregada';
END
ELSE
    PRINT '✓ Columna ticket_numero ya existe';
GO

-- Agregar columna bascula_id
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'bascula_id')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [bascula_id] BIGINT NULL;
    PRINT '✓ Columna bascula_id agregada';
END
ELSE
    PRINT '✓ Columna bascula_id ya existe';
GO

-- Agregar columna analisis_id
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'analisis_id')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [analisis_id] BIGINT NULL;
    PRINT '✓ Columna analisis_id agregada';
END
ELSE
    PRINT '✓ Columna analisis_id ya existe';
GO

-- Agregar columna fecha_hora
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'fecha_hora')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [fecha_hora] DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET();
    PRINT '✓ Columna fecha_hora agregada';
END
ELSE
    PRINT '✓ Columna fecha_hora ya existe';
GO

-- Agregar columna productor
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'productor')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [productor] NVARCHAR(255) NULL;
    PRINT '✓ Columna productor agregada';
END
ELSE
    PRINT '✓ Columna productor ya existe';
GO

-- Agregar columna telefono
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'telefono')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [telefono] NVARCHAR(20) NULL;
    PRINT '✓ Columna telefono agregada';
END
ELSE
    PRINT '✓ Columna telefono ya existe';
GO

-- Agregar columna t_productor
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 't_productor')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [t_productor] NVARCHAR(20) NULL;
    PRINT '✓ Columna t_productor agregada';
END
ELSE
    PRINT '✓ Columna t_productor ya existe';
GO

-- Agregar columna comprador
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'comprador')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [comprador] NVARCHAR(200) NULL;
    PRINT '✓ Columna comprador agregada';
END
ELSE
    PRINT '✓ Columna comprador ya existe';
GO

-- Agregar columna origen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'origen')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [origen] NVARCHAR(150) NULL;
    PRINT '✓ Columna origen agregada';
END
ELSE
    PRINT '✓ Columna origen ya existe';
GO

-- Agregar columna calibre
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'calibre')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [calibre] NVARCHAR(20) NULL;
    PRINT '✓ Columna calibre agregada';
END
ELSE
    PRINT '✓ Columna calibre ya existe';
GO

-- Agregar columna humedad
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'humedad')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [humedad] DECIMAL(5,2) NULL;
    PRINT '✓ Columna humedad agregada';
END
ELSE
    PRINT '✓ Columna humedad ya existe';
GO

-- Agregar columna peso_bruto
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'peso_bruto')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [peso_bruto] DECIMAL(12,2) NULL;
    PRINT '✓ Columna peso_bruto agregada';
END
ELSE
    PRINT '✓ Columna peso_bruto ya existe';
GO

-- Agregar columna tara
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'tara')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [tara] DECIMAL(12,2) NULL;
    PRINT '✓ Columna tara agregada';
END
ELSE
    PRINT '✓ Columna tara ya existe';
GO

-- Agregar columna peso_neto
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'peso_neto')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [peso_neto] DECIMAL(12,2) NULL;
    PRINT '✓ Columna peso_neto agregada';
END
ELSE
    PRINT '✓ Columna peso_neto ya existe';
GO

-- Agregar columna precio_base_usd
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'precio_base_usd')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [precio_base_usd] DECIMAL(12,2) NULL;
    PRINT '✓ Columna precio_base_usd agregada';
END
ELSE
    PRINT '✓ Columna precio_base_usd ya existe';
GO

-- Agregar columna tipo_cambio
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'tipo_cambio')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [tipo_cambio] DECIMAL(10,4) NULL;
    PRINT '✓ Columna tipo_cambio agregada';
END
ELSE
    PRINT '✓ Columna tipo_cambio ya existe';
GO

-- Agregar columna precio_mxn
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'precio_mxn')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [precio_mxn] DECIMAL(12,2) NULL;
    PRINT '✓ Columna precio_mxn agregada';
END
ELSE
    PRINT '✓ Columna precio_mxn ya existe';
GO

-- Agregar columna descuento_kg_ton
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'descuento_kg_ton')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [descuento_kg_ton] DECIMAL(10,2) NULL;
    PRINT '✓ Columna descuento_kg_ton agregada';
END
ELSE
    PRINT '✓ Columna descuento_kg_ton ya existe';
GO

-- Agregar columna kg_a_liquidar
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'kg_a_liquidar')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [kg_a_liquidar] DECIMAL(12,2) NULL;
    PRINT '✓ Columna kg_a_liquidar agregada';
END
ELSE
    PRINT '✓ Columna kg_a_liquidar ya existe';
GO

-- Agregar columna importe_total
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'importe_total')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [importe_total] DECIMAL(15,2) NULL;
    PRINT '✓ Columna importe_total agregada';
END
ELSE
    PRINT '✓ Columna importe_total ya existe';
GO

-- Agregar columna observaciones
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'observaciones')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [observaciones] NVARCHAR(MAX) NULL;
    PRINT '✓ Columna observaciones agregada';
END
ELSE
    PRINT '✓ Columna observaciones ya existe';
GO

-- Agregar columna status
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'status')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [status] NVARCHAR(50) NULL DEFAULT 'Pendiente';
    PRINT '✓ Columna status agregada';
END
ELSE
    PRINT '✓ Columna status ya existe';
GO

-- Agregar columna created_at
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'created_at')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [created_at] DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET();
    PRINT '✓ Columna created_at agregada';
END
ELSE
    PRINT '✓ Columna created_at ya existe';
GO

-- Agregar columna updated_at
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[boletas]') AND name = 'updated_at')
BEGIN
    ALTER TABLE [dbo].[boletas]
    ADD [updated_at] DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET();
    PRINT '✓ Columna updated_at agregada';
END
ELSE
    PRINT '✓ Columna updated_at ya existe';
GO

-- Crear índice único en folio si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Boletas_Folio')
BEGIN
    CREATE UNIQUE INDEX UQ_Boletas_Folio
    ON dbo.boletas(folio)
    WHERE folio IS NOT NULL;
    PRINT '✓ Índice único UQ_Boletas_Folio creado';
END
ELSE
    PRINT '✓ Índice UQ_Boletas_Folio ya existe';
GO

-- Crear índice en bascula_id
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Boletas_BasculaId')
BEGIN
    CREATE INDEX IX_Boletas_BasculaId
    ON dbo.boletas(bascula_id);
    PRINT '✓ Índice IX_Boletas_BasculaId creado';
END
ELSE
    PRINT '✓ Índice IX_Boletas_BasculaId ya existe';
GO

-- Crear índice en analisis_id
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Boletas_AnalisisId')
BEGIN
    CREATE INDEX IX_Boletas_AnalisisId
    ON dbo.boletas(analisis_id);
    PRINT '✓ Índice IX_Boletas_AnalisisId creado';
END
ELSE
    PRINT '✓ Índice IX_Boletas_AnalisisId ya existe';
GO

-- Crear índice en sede_id
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Boletas_SedeId')
BEGIN
    CREATE INDEX IX_Boletas_SedeId
    ON dbo.boletas(sede_id);
    PRINT '✓ Índice IX_Boletas_SedeId creado';
END
ELSE
    PRINT '✓ Índice IX_Boletas_SedeId ya existe';
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado exitosamente';
PRINT 'Columnas e índices agregados a la tabla boletas';
PRINT '=====================================================';
GO
