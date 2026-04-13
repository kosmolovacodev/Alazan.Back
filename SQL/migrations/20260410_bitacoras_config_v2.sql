-- ============================================================
-- Migración v2: dia_semana + tabla NIP global
-- Fecha: 2026-04-10
-- ============================================================

-- Añadir dia_semana a bitacoras_config
ALTER TABLE dbo.bitacoras_config
    ADD dia_semana NVARCHAR(20) NULL;  -- Lunes|Martes|Miércoles|Jueves|Viernes|Sábado|Domingo

-- Tabla de configuración global de firma NIP
CREATE TABLE dbo.bitacoras_nip_config (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    longitud_nip    INT NOT NULL DEFAULT 4,   -- 4 | 6
    intentos        INT NOT NULL DEFAULT 3,   -- 2 | 3 | 5
    tiempo_bloqueo  INT NOT NULL DEFAULT 15,  -- 5 | 15 | 30 (minutos)
    updated_at      DATETIME NULL
);

-- Insertar fila única por defecto
INSERT INTO dbo.bitacoras_nip_config (longitud_nip, intentos, tiempo_bloqueo, updated_at)
VALUES (4, 3, 15, GETDATE());
