-- ============================================================
-- Migración: Bitácoras Dinámicas
-- Fecha: 2026-04-13
-- Descripción:
--   1. Tablas de metadatos: secciones, definicion, columnas
--   2. Vistas SQL que conectan bitácoras "linked" con tablas operacionales
--   3. Seed data: todas las secciones y bitácoras existentes
-- ============================================================

-- ─────────────────────────────────────────────────────────────
-- 1. TABLAS DE METADATOS
-- ─────────────────────────────────────────────────────────────

CREATE TABLE dbo.bitacoras_secciones (
    codigo  NVARCHAR(10)  NOT NULL PRIMARY KEY,
    nombre  NVARCHAR(100) NOT NULL,
    icono   NVARCHAR(50)  NOT NULL DEFAULT 'menu_book',
    color   NVARCHAR(20)  NOT NULL DEFAULT '#607D8B',
    orden   INT           NOT NULL DEFAULT 0,
    activo  BIT           NOT NULL DEFAULT 1
);

CREATE TABLE dbo.bitacoras_definicion (
    codigo          NVARCHAR(20)  NOT NULL PRIMARY KEY,
    seccion_codigo  NVARCHAR(10)  NOT NULL REFERENCES dbo.bitacoras_secciones(codigo),
    nombre          NVARCHAR(200) NOT NULL,
    tipo            NVARCHAR(20)  NOT NULL DEFAULT 'manual',
        -- 'linked' = datos vienen de vista SQL operacional
        -- 'manual' = datos vienen de bitacoras_registros (formulario)
    fuente_query    NVARCHAR(100) NULL,   -- nombre de la vista para tipo='linked'
    orden           INT           NOT NULL DEFAULT 0,
    activo          BIT           NOT NULL DEFAULT 1
);

CREATE TABLE dbo.bitacoras_columnas (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    codigo_bitacora NVARCHAR(20)  NOT NULL,
    campo           NVARCHAR(100) NOT NULL,
    label           NVARCHAR(200) NOT NULL,
    tipo_dato       NVARCHAR(50)  NOT NULL DEFAULT 'texto',
        -- texto | numero | fecha | badge | si_no
    es_meta         BIT           NOT NULL DEFAULT 0,
        -- 1 = campo de metadatos (fecha, status), no aparece en columnas de datos
    orden           INT           NOT NULL DEFAULT 0,
    visible         BIT           NOT NULL DEFAULT 1
        -- 0 = oculto en la UI (la fila permanece en BD para recuperarla luego)
);

CREATE INDEX IX_bit_columnas_codigo ON dbo.bitacoras_columnas (codigo_bitacora);
GO

-- ─────────────────────────────────────────────────────────────
-- 2. VISTAS OPERACIONALES (tipo = 'linked')
-- ─────────────────────────────────────────────────────────────

-- ── FO-HC-IMP-002: Bitácora de Registro de Recepción de Grano ──
CREATE OR ALTER VIEW dbo.vw_bitacora_FO_HC_IMP_002 AS
SELECT
    b.id,
    b.sede_id,
    CONVERT(date, b.fecha_hora)   AS fecha,
    'Pendiente'                   AS status,

    -- Campos de datos (coinciden con bitacoras_columnas.campo)
    CASE
        WHEN p.tipo_persona = 'Moral'  THEN p.atiende
        WHEN p.tipo_persona = 'Fisica' THEN p.nombre
        ELSE COALESCE(p.atiende, p.nombre)
    END                           AS productor_cliente,
    CAST(b.placas AS NVARCHAR(50)) AS placas,
    COALESCE(g.nombre, '')        AS tipo_grano,
    b.ticket_numero               AS ticket,
    b.peso_bruto_kg               AS peso_bruto,
    b.tara_kg                     AS tara,
    b.peso_neto_kg                AS peso_neto,
    COALESCE(
        JSON_VALUE(b.datos_adicionales, '$.observaciones'),
        ''
    )                             AS observaciones

FROM dbo.bascula_recepciones b
LEFT JOIN dbo.productores    p ON b.productor_id = p.id
LEFT JOIN dbo.granos_catalogo g ON b.grano_id    = g.id;
GO

-- ── FO-HC-IMP-003: Bitácora de Análisis de Grano ──────────────
CREATE OR ALTER VIEW dbo.vw_bitacora_FO_HC_IMP_003 AS
SELECT
    a.id,
    a.sede_id,
    CONVERT(date, a.created_at)   AS fecha,
    'Pendiente'                   AS status,

    b.ticket_numero               AS ticket,
    CAST(b.placas AS NVARCHAR(50)) AS placas,
    COALESCE(
        JSON_VALUE(a.datos_adicionales, '$.color_apariencia'),
        ''
    )                             AS color_apariencia,
    COALESCE(
        JSON_VALUE(a.datos_adicionales, '$.olor'),
        'Normal'
    )                             AS olor,
    a.humedad,
    a.impurezas,
    COALESCE(
        JSON_VALUE(a.datos_adicionales, '$.infestacion_moho'),
        'N'
    )                             AS infestacion_moho,
    CASE
        WHEN a.total_danos IS NULL OR a.total_danos < 5 THEN 'APROBADO'
        ELSE 'RECHAZADO'
    END                           AS resultado_final,
    COALESCE(a.observaciones, '') AS observaciones

FROM dbo.analisis_calidad      a
LEFT JOIN dbo.bascula_recepciones b ON a.bascula_id = b.id;
GO

-- ── FO-HC-IMP-004: Bitácora de Volcado y Evaluación Visual ────
CREATE OR ALTER VIEW dbo.vw_bitacora_FO_HC_IMP_004 AS
SELECT
    v.id,
    v.sede_id,
    CONVERT(date, v.fecha_hora_volcado) AS fecha,
    'Pendiente'                         AS status,

    v.ticket_numero                     AS ticket,
    COALESCE(
        JSON_VALUE(v.datos_adicionales, '$.condicion_tolva'),
        ''
    )                                   AS condicion_tolva,
    COALESCE(
        JSON_VALUE(v.datos_adicionales, '$.condicion_grano'),
        ''
    )                                   AS condicion_grano,
    COALESCE(v.bodega_ubicacion + ' - ' + v.silo_numero, v.bodega_ubicacion, v.silo_numero, '')
                                        AS silo_asignado,
    COALESCE(v.observaciones, '')       AS observaciones

FROM dbo.volcado_bodega v;
GO

-- ─────────────────────────────────────────────────────────────
-- 3. SEED DATA — SECCIONES
-- ─────────────────────────────────────────────────────────────

INSERT INTO dbo.bitacoras_secciones (codigo, nombre, icono, color, orden) VALUES
    ('SEC-01', 'Báscula',    'scale',                  '#e65100', 1),
    ('SEC-02', 'Volcado',    'move_to_inbox',           '#f59e0b', 2),
    ('SEC-03', 'Proceso',    'precision_manufacturing', '#3b82f6', 3),
    ('SEC-04', 'Almacén',    'warehouse',               '#14b8a6', 4),
    ('SEC-05', 'Despacho',   'local_shipping',          '#8b5cf6', 5),
    ('SEC-06', 'Inocuidad',  'health_and_safety',       '#22c55e', 6);

-- ─────────────────────────────────────────────────────────────
-- 4. SEED DATA — DEFINICIÓN DE BITÁCORAS
-- ─────────────────────────────────────────────────────────────

-- SEC-01 Báscula
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-002', 'SEC-01', 'BITÁCORA DE REGISTRO DE RECEPCIÓN DE GRANO',         'linked', 'vw_bitacora_FO_HC_IMP_002', 1),
    ('FO-HC-IMP-003', 'SEC-01', 'BITÁCORA DE ANÁLISIS DE GRANO',                       'linked', 'vw_bitacora_FO_HC_IMP_003', 2),
    ('FO-HC-IMP-011', 'SEC-01', 'BITÁCORA DE BÁSCULA – SALIDA',                        'manual', NULL,                        3),
    ('FO-HC-IMP-014', 'SEC-01', 'BITÁCORA DE HIGIENE PERSONAL Y USO DE EPP',           'manual', NULL,                        4),
    ('FO-HC-IMP-019', 'SEC-01', 'BITÁCORA DE LIMPIEZA DE BÁSCULA Y ZONA DE MUESTREO', 'manual', NULL,                        5),
    ('FO-HC-IMP-020', 'SEC-01', 'BITÁCORA DE LIMPIEZA DE SANITARIOS Y COMEDOR',        'manual', NULL,                        6),
    ('FO-HC-IMP-023', 'SEC-01', 'BITÁCORA DE RECOLECCIÓN Y DISPOSICIÓN DE RESIDUOS',  'manual', NULL,                        7);

-- SEC-02 Volcado
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-004', 'SEC-02', 'BITÁCORA DE VOLCADO Y EVALUACIÓN VISUAL DEL GRANO',          'linked', 'vw_bitacora_FO_HC_IMP_004', 1),
    ('FO-HC-IMP-009', 'SEC-02', 'BITÁCORA DE MONITOREO DE TEMPERATURA Y HUMEDAD',              'manual', NULL,                        2),
    ('FO-HC-IMP-015', 'SEC-02', 'BITÁCORA DE LIMPIEZA DE TOLVA, ELEVADORES Y VOLCADOR',        'manual', NULL,                        3),
    ('FO-HC-IMP-016', 'SEC-02', 'BITÁCORA DE LIMPIEZA DE SILOS METÁLICOS',                     'manual', NULL,                        4),
    ('FO-HC-IMP-021', 'SEC-02', 'BITÁCORA DE CONTROL Y APLICACIÓN DE PLAGUICIDAS',             'manual', NULL,                        5);

-- Insertar FO-HC-IMP-014 y FO-HC-IMP-020 para SEC-02 con codigo distinto para evitar PK dup
-- Nota: misma bitácora puede aparecer en múltiples secciones; en ese caso se usa un sufijo de seccion
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-014-V', 'SEC-02', 'BITÁCORA DE HIGIENE PERSONAL Y USO DE EPP',          'manual', NULL, 6),
    ('FO-HC-IMP-020-V', 'SEC-02', 'BITÁCORA DE LIMPIEZA DE SANITARIOS Y COMEDOR',        'manual', NULL, 7);

-- SEC-03 Proceso
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-005', 'SEC-03', 'BITÁCORA DE PRODUCCIÓN Y CLASIFICACIÓN (GARBANZO / FRIJOL)',      'manual', NULL, 1),
    ('FO-HC-IMP-006', 'SEC-03', 'BITÁCORA DE CONTROL DE CALIDAD DEL PRODUCTO TERMINADO',           'manual', NULL, 2),
    ('FO-HC-IMP-017', 'SEC-03', 'BITÁCORA DE VERIFICACIÓN DE EQUIPOS Y ÁREAS DE PROCESO LIMPIAS', 'manual', NULL, 3),
    ('FO-HC-IMP-014-P', 'SEC-03', 'BITÁCORA DE HIGIENE PERSONAL Y USO DE EPP',                    'manual', NULL, 4);

-- SEC-04 Almacén
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-007', 'SEC-04', 'BITÁCORA DE DESCARGA MANUAL Y ALMACENAMIENTO DE FRIJOL', 'manual', NULL, 1),
    ('FO-HC-IMP-008', 'SEC-04', 'BITÁCORA DE ALMACENAMIENTO FINAL DE PRODUCTO',           'manual', NULL, 2),
    ('FO-HC-IMP-009-A', 'SEC-04', 'BITÁCORA DE MONITOREO DE TEMPERATURA Y HUMEDAD',       'manual', NULL, 3),
    ('FO-HC-IMP-014-A', 'SEC-04', 'BITÁCORA DE HIGIENE PERSONAL Y USO DE EPP',            'manual', NULL, 4),
    ('FO-HC-IMP-018', 'SEC-04', 'BITÁCORA DE LIMPIEZA DEL ALMACÉN',                       'manual', NULL, 5);

-- SEC-05 Despacho
INSERT INTO dbo.bitacoras_definicion (codigo, seccion_codigo, nombre, tipo, fuente_query, orden) VALUES
    ('FO-HC-IMP-010', 'SEC-05', 'BITÁCORA DE VERIFICACIÓN DE UNIDADES DE TRANSPORTE',    'manual', NULL, 1),
    ('FO-HC-IMP-014-D', 'SEC-05', 'BITÁCORA DE HIGIENE PERSONAL Y USO DE EPP',           'manual', NULL, 2),
    ('FO-HC-IMP-022', 'SEC-05', 'BITÁCORA DE DESPACHO Y SALIDA DE PRODUCTO',              'manual', NULL, 3);

-- SEC-06 Inocuidad (por definir)
-- (vacía por ahora, se agregará conforme se definan)

-- ─────────────────────────────────────────────────────────────
-- 5. SEED DATA — COLUMNAS DE BITÁCORAS
-- ─────────────────────────────────────────────────────────────

-- ── FO-HC-IMP-002 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-002', 'fecha',              'Fecha',                'fecha',  1, 1),
    ('FO-HC-IMP-002', 'productor_cliente',  'Productor/Cliente',    'texto',  0, 2),
    ('FO-HC-IMP-002', 'placas',             'Placas de Camión',     'texto',  0, 3),
    ('FO-HC-IMP-002', 'tipo_grano',         'Tipo de Grano',        'badge',  0, 4),
    ('FO-HC-IMP-002', 'ticket',             '# Ticket o Boleta',    'badge',  0, 5),
    ('FO-HC-IMP-002', 'peso_bruto',         'Peso Bruto (kg)',       'numero', 0, 6),
    ('FO-HC-IMP-002', 'tara',               'Tara (kg)',             'numero', 0, 7),
    ('FO-HC-IMP-002', 'peso_neto',          'Peso Neto (kg)',        'numero', 0, 8),
    ('FO-HC-IMP-002', 'observaciones',      'Observaciones',         'texto',  0, 9);

-- ── FO-HC-IMP-003 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-003', 'ticket',             'Ticket / No. Boleta',       'badge',  0, 1),
    ('FO-HC-IMP-003', 'placas',             'Placas de Camión',          'texto',  0, 2),
    ('FO-HC-IMP-003', 'color_apariencia',   'Color / Apariencia',        'texto',  0, 3),
    ('FO-HC-IMP-003', 'olor',               'Olor',                      'texto',  0, 4),
    ('FO-HC-IMP-003', 'humedad',            'Humedad del Grano (%)',      'numero', 0, 5),
    ('FO-HC-IMP-003', 'impurezas',          'Impurezas (%)',              'numero', 0, 6),
    ('FO-HC-IMP-003', 'infestacion_moho',   'Infestación / Moho (S/N)',   'si_no',  0, 7),
    ('FO-HC-IMP-003', 'resultado_final',    'Resultado Final',            'badge',  0, 8),
    ('FO-HC-IMP-003', 'observaciones',      'Observaciones',              'texto',  0, 9);

-- ── FO-HC-IMP-004 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-004', 'fecha',              'Fecha',                                    'fecha',  1, 1),
    ('FO-HC-IMP-004', 'ticket',             'Ticket / No. Boleta',                       'badge',  0, 2),
    ('FO-HC-IMP-004', 'condicion_tolva',    'Condición de Tolva antes de Volcado',       'texto',  0, 3),
    ('FO-HC-IMP-004', 'condicion_grano',    'Condición del Grano Descargado',            'texto',  0, 4),
    ('FO-HC-IMP-004', 'silo_asignado',      'Silo Asignado',                             'texto',  0, 5),
    ('FO-HC-IMP-004', 'observaciones',      'Observaciones',                             'texto',  0, 6);

-- ── FO-HC-IMP-009 (Temperatura/Humedad) ──────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-009', 'fecha',              'Fecha',                              'fecha',        0, 1),
    ('FO-HC-IMP-009', 'hora',               'Hora',                               'texto',        0, 2),
    ('FO-HC-IMP-009', 'area_silo',          'Área o Silo Monitoreado',             'texto',        0, 3),
    ('FO-HC-IMP-009', 'temperatura',        'Temperatura (°C)',                    'numero',       0, 4),
    ('FO-HC-IMP-009', 'humedad_relativa',   'Humedad Relativa (%)',                'numero',       0, 5),
    ('FO-HC-IMP-009', 'condicion',          'Condición del Grano / Obs. Visual',  'texto_color',  0, 6),
    ('FO-HC-IMP-009', 'accion_correctiva',  'Acción Correctiva Aplicada',         'texto_color',  0, 7),
    ('FO-HC-IMP-009', 'responsable',        'Nombre y Firma del Responsable',      'texto',        0, 8);

-- ── FO-HC-IMP-009-A (alias Almacén) ──────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-009-A', 'fecha',            'Fecha',                              'fecha',        0, 1),
    ('FO-HC-IMP-009-A', 'hora',             'Hora',                               'texto',        0, 2),
    ('FO-HC-IMP-009-A', 'area_silo',        'Área o Silo Monitoreado',             'texto',        0, 3),
    ('FO-HC-IMP-009-A', 'temperatura',      'Temperatura (°C)',                    'numero',       0, 4),
    ('FO-HC-IMP-009-A', 'humedad_relativa', 'Humedad Relativa (%)',                'numero',       0, 5),
    ('FO-HC-IMP-009-A', 'condicion',        'Condición del Grano / Obs. Visual',  'texto_color',  0, 6),
    ('FO-HC-IMP-009-A', 'accion_correctiva','Acción Correctiva Aplicada',         'texto_color',  0, 7),
    ('FO-HC-IMP-009-A', 'responsable',      'Nombre y Firma del Responsable',      'texto',        0, 8);

-- ── FO-HC-IMP-011 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-011', 'fecha',              'Fecha',                'fecha',  1, 1),
    ('FO-HC-IMP-011', 'ticket_folio',       'Ticket / Folio',       'badge',  0, 2),
    ('FO-HC-IMP-011', 'producto',           'Producto',              'texto',  0, 3),
    ('FO-HC-IMP-011', 'lote',               'Lote',                  'badge',  0, 4),
    ('FO-HC-IMP-011', 'peso_bruto',         'Peso Bruto (kg)',       'numero', 0, 5),
    ('FO-HC-IMP-011', 'tara',               'Tara (kg)',             'numero', 0, 6),
    ('FO-HC-IMP-011', 'peso_neto',          'Peso Neto (kg)',        'numero', 0, 7),
    ('FO-HC-IMP-011', 'cliente_destino',    'Cliente / Destino',    'texto',  0, 8),
    ('FO-HC-IMP-011', 'observaciones',      'Observaciones',         'texto',  0, 9);

-- ── FO-HC-IMP-005 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-005', 'fecha',              'Fecha',                             'fecha',  1, 1),
    ('FO-HC-IMP-005', 'orden_lote',         'Orden de Producción / Lote',         'badge',  0, 2),
    ('FO-HC-IMP-005', 'producto',           'Producto',                           'texto',  0, 3),
    ('FO-HC-IMP-005', 'num_tren',           '# Tren',                             'texto',  0, 4),
    ('FO-HC-IMP-005', 'silo_origen',        'Silo Origen',                        'texto',  0, 5),
    ('FO-HC-IMP-005', 'cant_procesada',     'Cantidad Procesada (kg)',             'numero', 0, 6),
    ('FO-HC-IMP-005', 'prod_clasificado',   'Producto Clasificado (kg)',           'numero', 0, 7),
    ('FO-HC-IMP-005', 'subproducto',        'Subproducto / Rezaga (kg)',           'numero', 0, 8),
    ('FO-HC-IMP-005', 'desecho',            'Desecho (kg)',                        'numero', 0, 9),
    ('FO-HC-IMP-005', 'rendimiento',        'Rendimiento (%)',                     'numero', 0, 10),
    ('FO-HC-IMP-005', 'observaciones',      'Observaciones',                       'texto',  0, 11);

-- ── FO-HC-IMP-006 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-006', 'fecha',               'Fecha',               'fecha',  1, 1),
    ('FO-HC-IMP-006', 'envasado',            'Envasado',             'texto',  0, 2),
    ('FO-HC-IMP-006', 'producto',            'Producto',             'texto',  0, 3),
    ('FO-HC-IMP-006', 'cosecha',             'Cosecha',              'texto',  0, 4),
    ('FO-HC-IMP-006', 'proceso',             'Proceso',              'texto',  0, 5),
    ('FO-HC-IMP-006', 'silos',               'Silos',                'texto',  0, 6),
    ('FO-HC-IMP-006', 'variedad',            'Variedad',             'texto',  0, 7),
    ('FO-HC-IMP-006', 'parrillas_completas', 'Parrillas Completas',  'numero', 0, 8),
    ('FO-HC-IMP-006', 'parrillas_comenzadas','Parrillas Comenzadas', 'numero', 0, 9);

-- ── FO-HC-IMP-007 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-007', 'fecha',              'Fecha',                                        'fecha',  1, 1),
    ('FO-HC-IMP-007', 'ticket',             'Ticket / Boleta No.',                           'badge',  0, 2),
    ('FO-HC-IMP-007', 'condicion_camion',   'Condición de Camión Antes de Descarga',         'texto',  0, 3),
    ('FO-HC-IMP-007', 'condicion_grano',    'Condición del Grano Descargado',                'texto',  0, 4),
    ('FO-HC-IMP-007', 'almacen_asignado',   'Almacén Asignado',                              'texto',  0, 5),
    ('FO-HC-IMP-007', 'observaciones',      'Observaciones',                                 'texto',  0, 6),
    ('FO-HC-IMP-007', 'resultado_accion',   'Resultado / Acción Tomada',                     'texto',  0, 7);

-- ── FO-HC-IMP-008 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-008', 'fecha',              'Fecha',                          'fecha',  1, 1),
    ('FO-HC-IMP-008', 'producto',           'Producto',                        'texto',  0, 2),
    ('FO-HC-IMP-008', 'presentacion',       'Presentación',                    'texto',  0, 3),
    ('FO-HC-IMP-008', 'variedad',           'Variedad',                        'texto',  0, 4),
    ('FO-HC-IMP-008', 'ubicacion',          'Ubicación',                       'texto',  0, 5),
    ('FO-HC-IMP-008', 'lote',               'Lote',                            'badge',  0, 6),
    ('FO-HC-IMP-008', 'cantidad',           'Cantidad Almacenada (kg / sacos)', 'numero', 0, 7),
    ('FO-HC-IMP-008', 'cond_producto',      'Condición del Producto',          'texto',  0, 8),
    ('FO-HC-IMP-008', 'cond_area',          'Condición del Área',              'texto',  0, 9),
    ('FO-HC-IMP-008', 'marbete_colocado',   'Marbete Colocado',                'si_no',  0, 10),
    ('FO-HC-IMP-008', 'codigo_marbete',     'Código del Marbete',              'texto',  0, 11),
    ('FO-HC-IMP-008', 'movimiento',         'Movimiento',                      'texto',  0, 12),
    ('FO-HC-IMP-008', 'observaciones',      'Observaciones',                   'texto',  0, 13);

-- ── FO-HC-IMP-010 ─────────────────────────────────────────────
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-010', 'fecha',               'Fecha',                                        'fecha',  1, 1),
    ('FO-HC-IMP-010', 'unidad_placas',       'Unidad / Placas',                               'texto',  0, 2),
    ('FO-HC-IMP-010', 'conductor',           'Conductor / Transportista',                     'texto',  0, 3),
    ('FO-HC-IMP-010', 'producto_despachado', 'Producto Despachado',                           'texto',  0, 4),
    ('FO-HC-IMP-010', 'lote',                'Lote de Producto',                              'badge',  0, 5),
    ('FO-HC-IMP-010', 'cantidad_peso',       'Cantidad / Peso (kg)',                          'numero', 0, 6),
    ('FO-HC-IMP-010', 'cond_vehiculo',       'Condición del Vehículo',                        'texto',  0, 7),
    ('FO-HC-IMP-010', 'resultado_verif',     'Resultado de Verificación',                     'badge',  0, 8),
    ('FO-HC-IMP-010', 'observaciones',       'Observaciones / Acción Correctiva',             'texto',  0, 9);

-- ── Bitácoras de higiene (FO-HC-IMP-014 y variantes) ──────────
-- Comparten columnas, para simpleza las definimos solo para FO-HC-IMP-014
-- Las variantes (-V, -P, -A, -D) usan las mismas columnas
INSERT INTO dbo.bitacoras_columnas (codigo_bitacora, campo, label, tipo_dato, es_meta, orden) VALUES
    ('FO-HC-IMP-014', 'fecha',              'Fecha',                    'fecha',  1, 1),
    ('FO-HC-IMP-014', 'turno',              'Turno',                    'texto',  0, 2),
    ('FO-HC-IMP-014', 'area',               'Área',                     'texto',  0, 3),
    ('FO-HC-IMP-014', 'nombre_trabajador',  'Nombre del Trabajador',    'texto',  0, 4),
    ('FO-HC-IMP-014', 'higiene_personal',   'Higiene Personal (S/N)',   'si_no',  0, 5),
    ('FO-HC-IMP-014', 'uso_epp',            'Uso de EPP (S/N)',          'si_no',  0, 6),
    ('FO-HC-IMP-014', 'observaciones',      'Observaciones',             'texto',  0, 7),
    ('FO-HC-IMP-014', 'supervisor',         'Supervisor',               'texto',  0, 8);
