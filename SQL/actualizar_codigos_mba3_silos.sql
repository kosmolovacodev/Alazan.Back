-- ============================================================
-- Asignación de códigos MBA3 a silos, tolvas y almacenes
--
-- IMPORTANTE: Ejecutar DESPUÉS de agregar_codigo_mba3_catalogos.sql
--
-- Mapa definitivo:
--   silos_calibre_catalogo  → SM1, SM2, SN1, SN2, SV1, SV2
--   silos_pulmon_catalogo   → SO3 (Silo Tolva, único)
--   catalogo_almacenes      → BM1-BM6 (Bodegas de frijol)
--   sedes_catalogo          → GML (Guamuchil), etc.
-- ============================================================

-- ═══ SILOS CALIBRE (sede 8) ═══════════════════════════════════
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SM1' WHERE id = 1;  -- Silo 1 (1250 ton)
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SM2' WHERE id = 5;  -- Silo 2 (1250 ton)
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SN1' WHERE id = 6;  -- Silo 3 (3000 ton)
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SN2' WHERE id = 7;  -- Silo 4 (3000 ton)
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SV1' WHERE id = 8;  -- Silo 5 (5000 ton)
UPDATE dbo.silos_calibre_catalogo SET codigo = 'SV2' WHERE id = 9;  -- Silo 6 (5000 ton)

-- ═══ SILOS PULMÓN / TOLVA (sede 8) ════════════════════════════
-- El Silo Tolva es ÚNICO en MBA3 → código SO3
-- TL1 es el registro principal; TL2 y TL3 son físicamente el mismo
-- o se asignan el mismo código si MBA3 los agrupa bajo SO3.
UPDATE dbo.silos_pulmon_catalogo SET codigo = 'SO3' WHERE id = 1;  -- TL1 → SO3

-- Si TL2 y TL3 también tienen código en MBA3, descomentar y ajustar:
-- UPDATE dbo.silos_pulmon_catalogo SET codigo = 'SO3' WHERE id = 2;  -- TL2
-- UPDATE dbo.silos_pulmon_catalogo SET codigo = 'SO3' WHERE id = 4;  -- TL3

-- ═══ ALMACENES / BODEGAS (catalogo_almacenes, sede 8) ══════════
-- Asignar BM1-BM6 a cada almacén según su nombre en MBA3.
-- Ajusta los IDs según los registros reales de tu BD.
-- Actualmente existen: id=1 (Prueba), id=2 (Almacen 1), id=3 (Almacen 2)
-- Cuando crees los registros definitivos (Bodega 1 a 6), actualiza aquí.

-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM1' WHERE id = 2;  -- Almacen 1 → BM1
-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM2' WHERE id = 3;  -- Almacen 2 → BM2
-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM3' WHERE id = ?;  -- Bodega 3  → BM3
-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM4' WHERE id = ?;  -- Bodega 4  → BM4
-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM5' WHERE id = ?;  -- Bodega 5  → BM5
-- UPDATE dbo.catalogo_almacenes SET codigo = 'BM6' WHERE id = ?;  -- Bodega 6  → BM6

-- ═══ SEDES (sucursal_origen) ════════════════════════════════════
-- Confirma el id de tu sede real y el código que MBA3 le asignó.
-- UPDATE dbo.sedes_catalogo SET codigo = 'GML' WHERE id = 8;   -- Guamuchil
-- UPDATE dbo.sedes_catalogo SET codigo = 'PRI' WHERE id = 9;   -- (sede 2, ajustar código)

-- ═══ VERIFICACIÓN FINAL ════════════════════════════════════════
SELECT 'silos_calibre' AS tabla, id, nombre, capacidad_toneladas AS cap_ton, codigo, sede_id
FROM dbo.silos_calibre_catalogo WHERE sede_id IN (8,9) ORDER BY sede_id, id;

SELECT 'silos_pulmon' AS tabla, id, nombre, tipo, codigo, sede_id
FROM dbo.silos_pulmon_catalogo ORDER BY sede_id, id;

SELECT 'almacenes' AS tabla, id, nombre_almacen AS nombre, codigo, sede_id
FROM dbo.catalogo_almacenes ORDER BY sede_id, id;

SELECT 'sedes' AS tabla, id, nombre_sede, codigo
FROM dbo.sedes_catalogo ORDER BY id;
