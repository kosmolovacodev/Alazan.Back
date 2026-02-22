-- ===================================================================
-- Script para agregar sede_id a tablas de catálogos de precio
-- ===================================================================

-- 1. Agregar columna sede_id a DescuentosCalibre_Catalogo
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.DescuentosCalibre_Catalogo') 
    AND name = 'sede_id'
)
BEGIN
    ALTER TABLE dbo.DescuentosCalibre_Catalogo
    ADD sede_id INT NOT NULL DEFAULT 8; -- Default a sede 8 (ajusta según tu necesidad)
    
    PRINT 'Columna sede_id agregada a DescuentosCalibre_Catalogo';
END
ELSE
BEGIN
    PRINT 'Columna sede_id ya existe en DescuentosCalibre_Catalogo';
END
GO

-- 2. Agregar columna sede_id a DescuentosPrecio_Catalogo
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.DescuentosPrecio_Catalogo') 
    AND name = 'sede_id'
)
BEGIN
    ALTER TABLE dbo.DescuentosPrecio_Catalogo
    ADD sede_id INT NOT NULL DEFAULT 8; -- Default a sede 8 (ajusta según tu necesidad)
    
    PRINT 'Columna sede_id agregada a DescuentosPrecio_Catalogo';
END
ELSE
BEGIN
    PRINT 'Columna sede_id ya existe en DescuentosPrecio_Catalogo';
END
GO

-- 3. OPCIONAL: Agregar FK constraints (recomendado)
-- Descomenta estas líneas si tienes una tabla 'sedes' con la que relacionar

/*
ALTER TABLE dbo.DescuentosCalibre_Catalogo
ADD CONSTRAINT FK_DescuentosCalibre_Sede
FOREIGN KEY (sede_id) REFERENCES sedes(id);

ALTER TABLE dbo.DescuentosPrecio_Catalogo
ADD CONSTRAINT FK_DescuentosPrecio_Sede
FOREIGN KEY (sede_id) REFERENCES sedes(id);
*/

-- 4. Verificar resultados
SELECT 'DescuentosCalibre_Catalogo' AS Tabla, COUNT(*) AS Registros, COUNT(DISTINCT sede_id) AS SedesUnicas
FROM dbo.DescuentosCalibre_Catalogo;

SELECT 'DescuentosPrecio_Catalogo' AS Tabla, COUNT(*) AS Registros, COUNT(DISTINCT sede_id) AS SedesUnicas
FROM dbo.DescuentosPrecio_Catalogo;

PRINT 'Script completado exitosamente';
GO
