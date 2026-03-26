-- Separar "tipo de dato" de "afecta exportación"
ALTER TABLE dbo.Configuracion_Campos_Pantallas
ADD afecta_exportacion BIT NOT NULL DEFAULT 0;

-- Los campos cal_1 y cal_2 ya existían como porcentaje_descuento_exportacion:
-- se migran a tipo 'porcentaje' + afecta_exportacion = 1
UPDATE dbo.Configuracion_Campos_Pantallas
SET tipo_dato = 'porcentaje',
    afecta_exportacion = 1
WHERE tipo_dato = 'porcentaje_descuento_exportacion';
