-- =====================================================
-- Script: Crear tablas para Gestión de Precios
-- Descripción: Crea las tablas necesarias para el módulo
--              de autorización y renegociación de precios
-- Fecha: 2026-02-04
-- =====================================================

USE [NombreDeTuBaseDeDatos]; -- Cambia esto por el nombre de tu BD
GO

-- =====================================================
-- 1. TABLA: Boletas_Precio
-- =====================================================
-- Almacena información de precios de boletas pendientes

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Boletas_Precio]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Boletas_Precio](
        [id] INT IDENTITY(1,1) NOT NULL,
        [sede_id] INT NOT NULL,
        [no_boleta] NVARCHAR(50) NOT NULL,
        [ticket] NVARCHAR(50) NULL,
        [fecha_registro] DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),

        -- Información del productor
        [productor_id] INT NULL,
        [productor_nombre] NVARCHAR(255) NULL,
        [telefono] NVARCHAR(20) NULL,

        -- Información del grano
        [comprador] NVARCHAR(100) NULL,
        [origen] NVARCHAR(100) NULL,
        [calibre] NVARCHAR(50) NULL,
        [tipo_grano] NVARCHAR(50) NULL,

        -- Pesos y descuentos
        [peso_bruto] DECIMAL(18,2) NULL,
        [tons_aprox] DECIMAL(18,3) NULL,
        [descuento_kg_ton] DECIMAL(18,2) NULL,

        -- Precio
        [precio_sugerido] DECIMAL(18,2) NULL,
        [precio_sugerido_codigo] NVARCHAR(20) NULL, -- Ej: "P001", "BASE"
        [precio_autorizado] DECIMAL(18,2) NULL,
        [precio_final] DECIMAL(18,2) NULL,

        -- Control de estado
        [estatus] NVARCHAR(50) NOT NULL DEFAULT 'Pendiente por Autorizar',
        -- Posibles valores:
        --   'Pendiente por Autorizar'
        --   'Precio Autorizado'
        --   'Pendiente por Negociar'
        --   'Precio Renegociado'
        --   'Rechazado'

        [tiempo_registro] DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [tiempo_autorizacion] DATETIMEOFFSET(7) NULL,
        [es_de_analisis] BIT NOT NULL DEFAULT 0,
        [autorizacion_automatica] BIT NOT NULL DEFAULT 0,

        -- Auditoría
        [usuario_registro] NVARCHAR(100) NULL,
        [usuario_autorizacion] NVARCHAR(100) NULL,
        [fecha_modificacion] DATETIMEOFFSET(7) NULL,

        -- Observaciones
        [observaciones] NVARCHAR(MAX) NULL,

        CONSTRAINT [PK_Boletas_Precio] PRIMARY KEY CLUSTERED ([id] ASC)
    );

    PRINT '✓ Tabla Boletas_Precio creada';
END
ELSE
    PRINT '✓ Tabla Boletas_Precio ya existe';
GO

-- Índices para Boletas_Precio
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Boletas_Precio_SedeId_Estatus')
BEGIN
    CREATE INDEX IX_Boletas_Precio_SedeId_Estatus
    ON dbo.Boletas_Precio(sede_id, estatus);
    PRINT '✓ Índice IX_Boletas_Precio_SedeId_Estatus creado';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Boletas_Precio_NoBoleta')
BEGIN
    CREATE INDEX IX_Boletas_Precio_NoBoleta
    ON dbo.Boletas_Precio(no_boleta);
    PRINT '✓ Índice IX_Boletas_Precio_NoBoleta creado';
END
GO

-- =====================================================
-- 2. TABLA: Historial_Precio
-- =====================================================
-- Registra todos los cambios de precio (log de auditoría)

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Historial_Precio]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Historial_Precio](
        [id] INT IDENTITY(1,1) NOT NULL,
        [boleta_precio_id] INT NOT NULL,
        [sede_id] INT NOT NULL,
        [no_boleta] NVARCHAR(50) NOT NULL,

        -- Cambio de precio
        [precio_anterior] DECIMAL(18,2) NULL,
        [precio_nuevo] DECIMAL(18,2) NOT NULL,
        [motivo_cambio] NVARCHAR(255) NULL,
        [tipo_accion] NVARCHAR(50) NOT NULL,
        -- Posibles valores: 'Autorización', 'Renegociación', 'Rechazo', 'Ajuste Manual'

        -- Auditoría
        [usuario] NVARCHAR(100) NULL,
        [fecha_cambio] DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT [PK_Historial_Precio] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_Historial_Precio_BoletaPrecio] FOREIGN KEY([boleta_precio_id])
            REFERENCES [dbo].[Boletas_Precio] ([id])
    );

    PRINT '✓ Tabla Historial_Precio creada';
END
ELSE
    PRINT '✓ Tabla Historial_Precio ya existe';
GO

-- Índice para Historial_Precio
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Historial_Precio_BoletaPrecioId')
BEGIN
    CREATE INDEX IX_Historial_Precio_BoletaPrecioId
    ON dbo.Historial_Precio(boleta_precio_id);
    PRINT '✓ Índice IX_Historial_Precio_BoletaPrecioId creado';
END
GO

-- =====================================================
-- 3. TABLA: Configuracion_Precio
-- =====================================================
-- Configuración de precios base por grano, calibre y sede

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Configuracion_Precio]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Configuracion_Precio](
        [id] INT IDENTITY(1,1) NOT NULL,
        [sede_id] INT NOT NULL,
        [grano_id] INT NOT NULL,
        [calibre] NVARCHAR(50) NOT NULL,

        -- Precio y penalizaciones
        [precio_base_ton] DECIMAL(18,2) NOT NULL,
        [codigo_precio] NVARCHAR(50) NULL, -- Ej: "P001", "BASE"
        [penalizacion_por_punto_pct] DECIMAL(5,2) NULL DEFAULT 0, -- Ej: 2.5 = reduce 2.5% por cada punto de descuento

        -- Control
        [activo] BIT NOT NULL DEFAULT 1,

        -- Configuración de autorización automática (opcional)
        [habilitar_autorizacion_automatica] BIT NOT NULL DEFAULT 0,
        [minutos_para_autorizacion] INT NOT NULL DEFAULT 30,

        -- Tolerancia de precios
        [tolerancia_precio_pct] DECIMAL(5,2) NULL DEFAULT 5.0, -- Ej: 5.00 = 5%
        [requiere_autorizacion_fuera_tolerancia] BIT NOT NULL DEFAULT 1,

        -- Auditoría
        [usuario_modificacion] NVARCHAR(100) NULL,
        [fecha_modificacion] DATETIMEOFFSET(7) NULL,
        [created_at] DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [updated_at] DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT [PK_Configuracion_Precio] PRIMARY KEY CLUSTERED ([id] ASC)
    );

    -- Constraint UNIQUE en (sede_id, grano_id, calibre)
    -- Solo puede haber un precio por combinación de sede-grano-calibre
    CREATE UNIQUE INDEX UQ_Configuracion_Precio_Sede_Grano_Calibre
    ON dbo.Configuracion_Precio(sede_id, grano_id, calibre) WHERE activo = 1;

    PRINT '✓ Tabla Configuracion_Precio creada';
END
ELSE
    PRINT '✓ Tabla Configuracion_Precio ya existe';
GO

-- Insertar datos de ejemplo (opcional - comentado)
-- INSERT INTO dbo.Configuracion_Precio (sede_id, grano_id, calibre, precio_base_ton, codigo_precio, penalizacion_por_punto_pct, activo)
-- VALUES
-- (1, 4, '44-46', 5500.00, 'CACAHUATE-44-46', 2.5, 1),
-- (1, 4, '48-50', 5800.00, 'CACAHUATE-48-50', 2.5, 1);
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Script completado exitosamente';
PRINT 'Tablas creadas para el módulo de Gestión de Precios';
PRINT '=====================================================';
GO
