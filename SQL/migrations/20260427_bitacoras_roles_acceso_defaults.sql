-- Pobla roles_acceso en bitacoras_secciones según el nombre de la sección.
-- Solo actualiza filas donde aún es NULL (no sobreescribe configuración manual).
-- Ejecutar después de 20260427_bitacoras_roles_acceso.sql
UPDATE dbo.bitacoras_secciones
SET roles_acceso = CASE
    WHEN LOWER(nombre) LIKE '%áscula%'   OR LOWER(nombre) LIKE '%ascula%'   THEN 'bascula'
    WHEN LOWER(nombre) LIKE '%olcado%'                                       THEN 'volcado'
    WHEN LOWER(nombre) LIKE '%roducci%'                                      THEN 'produccion'
    WHEN LOWER(nombre) LIKE '%lmac%'                                         THEN 'almacen'
    WHEN LOWER(nombre) LIKE '%espacho%'                                      THEN 'despacho'
    WHEN LOWER(nombre) LIKE '%nocuidad%'                                     THEN 'inocuidad'
    ELSE NULL  -- visible para todos
END
WHERE roles_acceso IS NULL;

-- Verificar resultado:
SELECT codigo, nombre, roles_acceso FROM dbo.bitacoras_secciones ORDER BY orden;
