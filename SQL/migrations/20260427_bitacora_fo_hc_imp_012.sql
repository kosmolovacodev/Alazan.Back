-- ============================================================
-- Migración: FO-HC-IMP-012 — Control de Resguardo de Bitácoras
-- Sección: SEC-06 Inocuidad
-- Firmas:  Supervisor → ESGI  |  Gerente → Revisión Administración
-- ============================================================

-- 1. Definición de la bitácora
IF NOT EXISTS (SELECT 1 FROM dbo.bitacoras_definicion WHERE codigo = 'FO-HC-IMP-012')
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden, activo)
VALUES ('FO-HC-IMP-012', 'SEC-06', 'CONTROL DE RESGUARDO DE BITÁCORAS', 'manual', NULL, 1, 1);

-- 2. Columnas
IF NOT EXISTS (SELECT 1 FROM dbo.bitacoras_columnas WHERE codigo_bitacora = 'FO-HC-IMP-012')
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden, visible) VALUES
    ('FO-HC-IMP-012', 'semana',          'Semana',                        'texto', 0,  1, 1),
    ('FO-HC-IMP-012', 'fecha',           'Fecha',                         'fecha', 0,  2, 1),
    ('FO-HC-IMP-012', 'codigo_fo_hc',    'Código FO-HC',                  'texto', 0,  3, 1),
    ('FO-HC-IMP-012', 'nombre_bitacora', 'Nombre de la Bitácora',         'texto', 0,  4, 1),
    ('FO-HC-IMP-012', 'area_proceso',    'Área o Proceso',                'texto', 0,  5, 1),
    ('FO-HC-IMP-012', 'responsable',     'Responsable de Llenado',        'texto', 0,  6, 1),
    ('FO-HC-IMP-012', 'fecha_entrega',   'Fecha de Entrega al ESGI',      'fecha', 0,  7, 1),
    ('FO-HC-IMP-012', 'conformidad',     'Conformidad del Registro',      'si_no', 0,  8, 1),
    ('FO-HC-IMP-012', 'observaciones',   'Observaciones',                 'texto', 0,  9, 1);

-- 3. Configuración de firmas y periodicidad
IF NOT EXISTS (SELECT 1 FROM dbo.bitacoras_config WHERE codigo_bitacora = 'FO-HC-IMP-012')
INSERT INTO dbo.bitacoras_config (
    codigo_bitacora, periodicidad,
    rol_operativo,  firmas_operativo,  etiqueta_operativo,
    rol_supervisor, firmas_supervisor, etiqueta_supervisor,
    rol_gerente,    firmas_gerente,    etiqueta_gerente,
    firma_recepcion, etiqueta_recepcion
) VALUES (
    'FO-HC-IMP-012', 'Semanal',
    0, 0, NULL,
    1, 1, 'ESGI',
    1, 1, 'Revisión Administración',
    0, NULL
);

-- 4. roles_acceso para que solo Inocuidad vea esta sección
UPDATE dbo.bitacoras_secciones
SET roles_acceso = 'inocuidad'
WHERE codigo = 'SEC-06' AND (roles_acceso IS NULL OR roles_acceso = '');

-- Verificar:
SELECT d.codigo, d.nombre, d.seccion_codigo, c.periodicidad,
       c.etiqueta_supervisor, c.etiqueta_gerente
FROM dbo.bitacoras_definicion d
JOIN dbo.bitacoras_config c ON c.codigo_bitacora = d.codigo
WHERE d.codigo = 'FO-HC-IMP-012';
