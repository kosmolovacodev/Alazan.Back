-- Elimina todos los documentos de bitácora para poder regenerarlos en pruebas.
-- EJECUTAR SOLO EN DESARROLLO. Borra firmas y documentos sin restricción.

BEGIN TRANSACTION;

DELETE FROM dbo.bitacoras_documento_firmas
WHERE documento_id IN (
    SELECT id FROM dbo.bitacoras_documentos
);

DELETE FROM dbo.bitacoras_documentos;

-- Verificar:
SELECT COUNT(*) AS documentos_restantes FROM dbo.bitacoras_documentos;

COMMIT;
