-- Ajustes FO-HC-IMP-009 y FO-HC-IMP-009-A
-- 1. fecha: es_meta = 0 para que aparezca en el formulario de nuevo registro
-- 2. condicion / accion_correctiva: tipo_dato = 'texto_color' (texto en color de sección)
-- 3. responsable: label corregido a "Nombre y Firma del Responsable"

UPDATE dbo.bitacoras_columnas
SET es_meta = 0
WHERE codigo_bitacora IN ('FO-HC-IMP-009', 'FO-HC-IMP-009-A')
  AND campo = 'fecha';

UPDATE dbo.bitacoras_columnas
SET tipo_dato = 'texto_color'
WHERE codigo_bitacora IN ('FO-HC-IMP-009', 'FO-HC-IMP-009-A')
  AND campo IN ('condicion', 'accion_correctiva');

UPDATE dbo.bitacoras_columnas
SET label = 'Nombre y Firma del Responsable'
WHERE codigo_bitacora IN ('FO-HC-IMP-009', 'FO-HC-IMP-009-A')
  AND campo = 'responsable';
