-- Agregar columna tipo_dato a Configuracion_Campos_Pantallas
-- Tipos posibles: texto | numero | porcentaje | porcentaje_descuento_exportacion
ALTER TABLE dbo.Configuracion_Campos_Pantallas
ADD tipo_dato VARCHAR(50) NULL DEFAULT 'texto';

-- Marcar los campos cal_1 y cal_2 (C# - 1% y C# - 2%) como descuentos de exportacion
UPDATE dbo.Configuracion_Campos_Pantallas
SET tipo_dato = 'porcentaje_descuento_exportacion'
WHERE clave_campo IN ('cal_1', 'cal_2');
