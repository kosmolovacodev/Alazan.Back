-- Tabla de precios por clasificación de grano (exclusivo GARBANZO)
CREATE TABLE dbo.precios_clasificacion (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    sede_id     INT NOT NULL,
    grano_id    INT NOT NULL,            -- 4 = Garbanzo
    nombre      VARCHAR(50) NOT NULL,    -- 'CAL. EXP', 'CAL 1', 'CAL 2'
    codigo      VARCHAR(20) NOT NULL,    -- 'CAL_EXP', 'CAL_1', 'CAL_2'
    precio_kg   DECIMAL(10,4) NOT NULL DEFAULT 0,
    activo      BIT NOT NULL DEFAULT 1,
    orden       INT NOT NULL DEFAULT 1,
    created_at  DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    updated_at  DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- Índice para consultas frecuentes
CREATE INDEX IX_precios_clasificacion_sede_grano
    ON dbo.precios_clasificacion (sede_id, grano_id);

-- Insertar las 3 clasificaciones base para cada sede que tenga garbanzo
-- Ajusta sede_id y grano_id según tus datos
-- Ejemplo para sede 8, garbanzo id 4:
INSERT INTO dbo.precios_clasificacion (sede_id, grano_id, nombre, codigo, precio_kg, activo, orden)
VALUES
    (8, 4, 'CAL. EXP', 'CAL_EXP', 0.00, 1, 1),
    (8, 4, 'CAL 1',    'CAL_1',   0.00, 1, 2),
    (8, 4, 'CAL 2',    'CAL_2',   0.00, 1, 3);
