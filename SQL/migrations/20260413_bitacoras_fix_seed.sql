-- ============================================================
-- Fix: Limpiar bitácoras extra del seed anterior
-- Fecha: 2026-04-13
-- Descripción:
--   Deja solo las bitácoras operacionales reconocidas en
--   ConfiguracionBitacoras.vue. Elimina las de higiene/limpieza
--   que se insertaron de más en 20260413_bitacoras_dinamicas.sql
-- ============================================================

-- Códigos que SÍ deben existir
-- SEC-01: 002, 003, 011
-- SEC-02: 004, 009
-- SEC-03: 005, 006
-- SEC-04: 007, 008, 009-A  (009 de Almacén usa sufijo -A como PK única)
-- SEC-05: 010
-- SEC-06: (sin bitácoras por ahora)

-- 1. Eliminar columnas de bitácoras que sobran
DELETE FROM dbo.bitacoras_columnas
WHERE codigo_bitacora NOT IN (
    'FO-HC-IMP-002',
    'FO-HC-IMP-003',
    'FO-HC-IMP-004',
    'FO-HC-IMP-005',
    'FO-HC-IMP-006',
    'FO-HC-IMP-007',
    'FO-HC-IMP-008',
    'FO-HC-IMP-009',
    'FO-HC-IMP-009-A',
    'FO-HC-IMP-010',
    'FO-HC-IMP-011'
);

-- 2. Eliminar definiciones que sobran
DELETE FROM dbo.bitacoras_definicion
WHERE codigo NOT IN (
    'FO-HC-IMP-002',
    'FO-HC-IMP-003',
    'FO-HC-IMP-004',
    'FO-HC-IMP-005',
    'FO-HC-IMP-006',
    'FO-HC-IMP-007',
    'FO-HC-IMP-008',
    'FO-HC-IMP-009',
    'FO-HC-IMP-009-A',
    'FO-HC-IMP-010',
    'FO-HC-IMP-011'
);

-- 3. Corregir órdenes para que queden consecutivos por sección
UPDATE dbo.bitacoras_definicion SET orden = 1 WHERE codigo = 'FO-HC-IMP-002';
UPDATE dbo.bitacoras_definicion SET orden = 2 WHERE codigo = 'FO-HC-IMP-003';
UPDATE dbo.bitacoras_definicion SET orden = 3 WHERE codigo = 'FO-HC-IMP-011';

UPDATE dbo.bitacoras_definicion SET orden = 1 WHERE codigo = 'FO-HC-IMP-004';
UPDATE dbo.bitacoras_definicion SET orden = 2 WHERE codigo = 'FO-HC-IMP-009';

UPDATE dbo.bitacoras_definicion SET orden = 1 WHERE codigo = 'FO-HC-IMP-005';
UPDATE dbo.bitacoras_definicion SET orden = 2 WHERE codigo = 'FO-HC-IMP-006';

UPDATE dbo.bitacoras_definicion SET orden = 1 WHERE codigo = 'FO-HC-IMP-007';
UPDATE dbo.bitacoras_definicion SET orden = 2 WHERE codigo = 'FO-HC-IMP-008';
UPDATE dbo.bitacoras_definicion SET orden = 3 WHERE codigo = 'FO-HC-IMP-009-A';

UPDATE dbo.bitacoras_definicion SET orden = 1 WHERE codigo = 'FO-HC-IMP-010';

-- Verificar resultado
SELECT d.seccion_codigo, d.codigo, d.nombre, d.tipo
FROM dbo.bitacoras_definicion d
ORDER BY d.seccion_codigo, d.orden;
