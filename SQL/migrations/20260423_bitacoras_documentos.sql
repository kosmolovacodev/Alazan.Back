-- Tabla de documentos diarios (uno por bitácora + sede + fecha)
IF OBJECT_ID('dbo.bitacoras_documentos', 'U') IS NULL
CREATE TABLE dbo.bitacoras_documentos (
    id              INT IDENTITY PRIMARY KEY,
    codigo_bitacora NVARCHAR(20) NOT NULL,
    sede_id         INT NOT NULL,
    fecha           DATE NOT NULL,
    status          NVARCHAR(20) DEFAULT 'Pendiente', -- 'Pendiente' | 'Firmado'
    created_at      DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_bitdoc UNIQUE(codigo_bitacora, sede_id, fecha)
);

-- Slots de firma por documento
IF OBJECT_ID('dbo.bitacoras_documento_firmas', 'U') IS NULL
CREATE TABLE dbo.bitacoras_documento_firmas (
    id              INT IDENTITY PRIMARY KEY,
    documento_id    INT NOT NULL REFERENCES dbo.bitacoras_documentos(id),
    rol_requerido   NVARCHAR(20) NOT NULL,
    etiqueta        NVARCHAR(100) NULL,
    orden           INT DEFAULT 0,
    usuario_id      INT NULL,
    nombre_firmante NVARCHAR(200) NULL,
    firma_texto     NVARCHAR(MAX) NULL,
    firmado_en      DATETIME NULL,
    CONSTRAINT UQ_bitdocfirma UNIQUE(documento_id, rol_requerido)
);

-- Etiquetas de display por rol en la config de bitácoras
IF COL_LENGTH('dbo.bitacoras_config', 'etiqueta_operativo') IS NULL
    ALTER TABLE dbo.bitacoras_config
        ADD etiqueta_operativo  NVARCHAR(100) NULL,
            etiqueta_supervisor NVARCHAR(100) NULL,
            etiqueta_gerente    NVARCHAR(100) NULL,
            etiqueta_recepcion  NVARCHAR(100) NULL;

-- NIP hasheado en usuarios (BCrypt)
IF COL_LENGTH('dbo.usuarios', 'nip_hash') IS NULL
    ALTER TABLE dbo.usuarios ADD nip_hash NVARCHAR(255) NULL;
