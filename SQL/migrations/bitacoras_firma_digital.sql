-- Migration: bitacoras_firma_digital
-- Agrega soporte de firma digital con NIP para el sistema de bitácoras

-- 1. NIP hasheado en usuarios (BCrypt, igual que la contraseña)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.usuarios') AND name = 'nip_hash')
    ALTER TABLE dbo.usuarios ADD nip_hash NVARCHAR(255) NULL;

-- 2. Columnas de seguimiento de PDF y firmas en bitacoras_registros
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.bitacoras_registros') AND name = 'pdf_path')
    ALTER TABLE dbo.bitacoras_registros ADD pdf_path NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.bitacoras_registros') AND name = 'pdf_generado_en')
    ALTER TABLE dbo.bitacoras_registros ADD pdf_generado_en DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.bitacoras_registros') AND name = 'pdf_generado_por')
    ALTER TABLE dbo.bitacoras_registros ADD pdf_generado_por NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.bitacoras_registros') AND name = 'firmas_requeridas')
    ALTER TABLE dbo.bitacoras_registros ADD firmas_requeridas INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.bitacoras_registros') AND name = 'firmas_completadas')
    ALTER TABLE dbo.bitacoras_registros ADD firmas_completadas INT NOT NULL DEFAULT 0;

-- 3. Tabla de slots de firma digital
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('dbo.bitacoras_firmas') AND type = 'U')
CREATE TABLE dbo.bitacoras_firmas (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    registro_id     INT           NOT NULL,
    rol_requerido   NVARCHAR(20)  NOT NULL,   -- 'operativo' | 'supervisor' | 'gerente' | 'recepcion'
    usuario_id      BIGINT        NULL,        -- usuario asignado para firmar este slot
    nombre_firmante NVARCHAR(200) NULL,        -- nombre capturado al firmar
    token_firma     NVARCHAR(100) NULL,        -- GUID único enviado por email
    token_expira    DATETIME      NULL,        -- token válido 48 horas
    token_usado     BIT           NOT NULL DEFAULT 0,
    firmado_en      DATETIME      NULL,
    ip_firma        NVARCHAR(50)  NULL,
    created_at      DATETIME      NOT NULL DEFAULT GETDATE()
);
