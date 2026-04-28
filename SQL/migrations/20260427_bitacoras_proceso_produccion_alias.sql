-- Unifica "proceso" y "produccion" como la misma área de acceso.
-- Las secciones que se llamen "Proceso" o "Producción" aceptan
-- usuarios con cualquiera de los dos keywords en su rol.

UPDATE dbo.bitacoras_secciones
SET roles_acceso = 'produccion,proceso'
WHERE LOWER(roles_acceso) = 'produccion'
   OR LOWER(nombre) LIKE '%roceso%'
   OR LOWER(nombre) LIKE '%roducci%';

-- Verificar:
SELECT codigo, nombre, roles_acceso FROM dbo.bitacoras_secciones ORDER BY orden;
