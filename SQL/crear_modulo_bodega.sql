-- ============================================================
-- MÓDULO BODEGA — Migración completa
-- Ejecutar en orden. Tablas nuevas; no modifica las existentes.
-- ============================================================

-- ─── CATÁLOGOS ────────────────────────────────────────────────────

CREATE TABLE dbo.cat_bodega (
    id         INT IDENTITY(1,1) PRIMARY KEY,
    sede_id    INT NOT NULL,
    clave      VARCHAR(10)   NOT NULL,
    nombre     NVARCHAR(100) NOT NULL,
    activo     BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_cat_bodega_clave_sede UNIQUE (sede_id, clave)
);
GO

CREATE TABLE dbo.cat_cuadrante (
    id         INT IDENTITY(1,1) PRIMARY KEY,
    sede_id    INT NOT NULL,
    clave      VARCHAR(10)   NOT NULL,
    nombre     NVARCHAR(100) NOT NULL,
    activo     BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_cat_cuadrante_clave_sede UNIQUE (sede_id, clave)
);
GO

CREATE TABLE dbo.cat_tipo_costal (
    id         INT IDENTITY(1,1) PRIMARY KEY,
    sede_id    INT NOT NULL,
    nombre     NVARCHAR(100) NOT NULL,
    activo     BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.cat_subproducto_bodega (
    id         INT IDENTITY(1,1) PRIMARY KEY,
    sede_id    INT NOT NULL,
    nombre     NVARCHAR(100) NOT NULL,
    activo     BIT NOT NULL DEFAULT 1
);
GO

-- ─── TABLAS OPERATIVAS ────────────────────────────────────────────

CREATE TABLE dbo.bodega_asignaciones (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    orden_id            INT NOT NULL,                -- FK a ordenesproduccion.id
    sede_id             INT NOT NULL,
    fecha               DATE NULL,
    status_asignacion   VARCHAR(50) NOT NULL DEFAULT 'No Asignado',
    fecha_creacion      DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    fecha_actualizacion DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE TABLE dbo.bodega_asignacion_items (
    id               INT IDENTITY(1,1) PRIMARY KEY,
    asignacion_id    INT NOT NULL REFERENCES dbo.bodega_asignaciones(id),
    tipo             VARCHAR(20)   NOT NULL DEFAULT 'producto',   -- 'producto' | 'subproducto'
    nombre           NVARCHAR(100) NOT NULL,
    presentacion_id  INT NULL,   -- FK a cat_presentacion_produccion
    tipo_costal_id   INT NULL,   -- FK a cat_tipo_costal
    cantidad_total   DECIMAL(18,2) NOT NULL DEFAULT 0,
    orden_item       INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.bodega_asignacion_detalle (
    id           INT IDENTITY(1,1) PRIMARY KEY,
    item_id      INT NOT NULL REFERENCES dbo.bodega_asignacion_items(id),
    bodega_id    INT NOT NULL,   -- FK a cat_bodega
    cuadrante_id INT NOT NULL,   -- FK a cat_cuadrante
    cantidad     DECIMAL(18,2) NOT NULL DEFAULT 0
);
GO

-- ─── ÍNDICES ──────────────────────────────────────────────────────

CREATE INDEX IX_bodega_asignaciones_orden   ON dbo.bodega_asignaciones (orden_id);
CREATE INDEX IX_bodega_asignaciones_sede    ON dbo.bodega_asignaciones (sede_id);
CREATE INDEX IX_bodega_asig_items_asig      ON dbo.bodega_asignacion_items (asignacion_id);
CREATE INDEX IX_bodega_asig_detalle_item    ON dbo.bodega_asignacion_detalle (item_id);
GO

-- ─── DATOS SEMILLA (ajustar sede_id según entorno) ────────────────
-- Reemplaza @sedeId con el ID de sede real antes de ejecutar.

DECLARE @sedeId INT = 1;   -- ← cambiar según la sede

-- Bodegas
INSERT INTO dbo.cat_bodega (sede_id, clave, nombre) VALUES
  (@sedeId, 'B1', 'Bodega Norte 1'),
  (@sedeId, 'B2', 'Bodega Norte 2'),
  (@sedeId, 'B3', 'Bodega Sur 1'),
  (@sedeId, 'B4', 'Bodega Sur 2'),
  (@sedeId, 'B5', 'Bodega Este'),
  (@sedeId, 'B6', 'Bodega Oeste');

-- Cuadrantes
INSERT INTO dbo.cat_cuadrante (sede_id, clave, nombre) VALUES
  (@sedeId, 'C1', 'Cuadrante Norte'),
  (@sedeId, 'C2', 'Cuadrante Sur'),
  (@sedeId, 'C3', 'Cuadrante Este'),
  (@sedeId, 'C4', 'Cuadrante Oeste');

-- Tipos de costal (ejemplo)
INSERT INTO dbo.cat_tipo_costal (sede_id, nombre) VALUES
  (@sedeId, 'Costal CHICKPEAS'),
  (@sedeId, 'Costal CHICKPEAS ALAZAN NUEVO'),
  (@sedeId, 'Costal BIZZ LAMI'),
  (@sedeId, 'Costal BIZZ BOPP'),
  (@sedeId, 'Costal BLANCO NVO'),
  (@sedeId, 'Costal USADO 25 KG'),
  (@sedeId, 'Costal USADO 50 KG');

-- Subproductos almacenables
INSERT INTO dbo.cat_subproducto_bodega (sede_id, nombre) VALUES
  (@sedeId, 'Rezaga de Criba'),
  (@sedeId, 'Granillo Precalibrador'),
  (@sedeId, 'Rezaga de Mesa'),
  (@sedeId, 'Rezaga Calibrador'),
  (@sedeId, 'Granillo Calibrador'),
  (@sedeId, 'Rezaga de Ojo');
GO

PRINT 'Módulo Bodega creado correctamente.';
