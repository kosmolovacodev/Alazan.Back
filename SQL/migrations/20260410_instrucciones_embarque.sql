-- ============================================================
-- Migración: Módulo Instrucciones de Embarque
-- Fecha: 2026-04-10
-- ============================================================

-- ── Catálogos ────────────────────────────────────────────────

CREATE TABLE dbo.cat_ie_presentacion (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    nombre      NVARCHAR(120)  NOT NULL,
    activo      BIT            NOT NULL DEFAULT 1
);

CREATE TABLE dbo.cat_ie_broker (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    nombre      NVARCHAR(200)  NOT NULL,
    activo      BIT            NOT NULL DEFAULT 1
);

CREATE TABLE dbo.cat_ie_lugar (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    nombre      NVARCHAR(200)  NOT NULL,
    activo      BIT            NOT NULL DEFAULT 1
);

CREATE TABLE dbo.cat_ie_plantilla (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    titulo      NVARCHAR(200)  NOT NULL,
    cuerpo      NVARCHAR(MAX)  NOT NULL,
    activo      BIT            NOT NULL DEFAULT 1
);

-- ── Instrucciones principales ─────────────────────────────────

CREATE TABLE dbo.instrucciones_embarque (
    id                    INT IDENTITY(1,1) PRIMARY KEY,
    no_instruccion        NVARCHAR(30)   NOT NULL,
    referencia_alazan     NVARCHAR(100)  NULL,          -- Ej. ALZ-XXXX-25
    fecha                 DATE           NOT NULL DEFAULT GETDATE(),
    cliente               NVARCHAR(200)  NULL,
    domicilio             NVARCHAR(300)  NULL,          -- Ciudad, País
    contrato              NVARCHAR(100)  NULL,
    broker                NVARCHAR(200)  NULL,          -- texto libre o del catálogo
    producto              NVARCHAR(30)   NOT NULL,      -- 'Garbanzo' | 'Frijol'
    calibre               NVARCHAR(100)  NULL,
    tons                  DECIMAL(10,2)  NOT NULL DEFAULT 0,
    precio_unitario       NVARCHAR(50)   NULL,          -- Ej. '1500 USD'
    presentacion_id       INT            NULL REFERENCES dbo.cat_ie_presentacion(id),
    fecha_embarque        NVARCHAR(100)  NULL,          -- Ej. 'Octubre' (texto libre)
    lugar_embarque        NVARCHAR(200)  NULL,          -- texto libre o del catálogo
    status_embarque       NVARCHAR(30)   NOT NULL DEFAULT 'Pendiente',
        -- Pendiente | En Tránsito | Embarcado | Cancelado
    status_documentacion  NVARCHAR(30)   NOT NULL DEFAULT 'Incompleto',
        -- Incompleto | Completo | Enviado
    condiciones_generales NVARCHAR(MAX)  NULL,          -- Ej. GAFTA 24/BBA/25 - Arbitration London
    condiciones_especiales NVARCHAR(MAX) NULL,          -- Una condición por línea
    plantilla_id          INT            NULL REFERENCES dbo.cat_ie_plantilla(id),
    sede_id               INT            NOT NULL DEFAULT 0,
    activo                BIT            NOT NULL DEFAULT 1,
    created_at            DATETIME       NOT NULL DEFAULT GETDATE(),
    updated_at            DATETIME       NULL
);

-- ── Datos semilla opcionales ──────────────────────────────────

INSERT INTO dbo.cat_ie_presentacion (nombre) VALUES
    ('25 kg Marco Alazán'),
    ('Bolsa 50 kg'),
    ('Bolsa 100 lbs'),
    ('Granel');

INSERT INTO dbo.cat_ie_lugar (nombre) VALUES
    ('Puerto de Guaymas'),
    ('Puerto de Manzanillo'),
    ('Puerto de Veracruz'),
    ('Frontera Nogales');

INSERT INTO dbo.cat_ie_broker (nombre) VALUES
    ('INTERCOUNTAGE');
