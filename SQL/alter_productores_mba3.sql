-- ============================================================
-- ALTER TABLE productores
-- Agregar todas las columnas del Maestro de Proveedores MBA3
-- Columnas ya existentes: id, nombre, telefono, rfc, correo,
--   origen_id, tipo_persona, banco_id, cuenta_clabe,
--   activo, created_at, updated_at, atiende, sede_id
-- ============================================================

ALTER TABLE dbo.productores ADD

    -- ── Identificadores MBA3 ─────────────────────────────────
    numero_erp                  INT             NULL,   -- "Numero" (ID secuencial MBA3)
    codigo_proveedor            VARCHAR(20)     NULL,   -- "Código Proveedor"

    -- ── Dirección ────────────────────────────────────────────
    direccion1                  VARCHAR(255)    NULL,   -- "Dirección 1"
    direccion2                  VARCHAR(255)    NULL,   -- "Dirección 2"
    pais                        VARCHAR(10)     NULL,   -- "País"          ej: MX
    estado                      VARCHAR(100)    NULL,   -- "Estado"         ej: SIN
    ciudad                      VARCHAR(100)    NULL,   -- "Ciudad"
    sector                      VARCHAR(100)    NULL,   -- "Sector"
    codigo_postal               VARCHAR(10)     NULL,   -- "Código Postal"
    numero_exterior             VARCHAR(20)     NULL,   -- "Numero Exterior"
    numero_interior             VARCHAR(20)     NULL,   -- "Número Interior"
    colonia                     VARCHAR(100)    NULL,   -- "Colonia"
    localidad                   VARCHAR(100)    NULL,   -- "Localidad"

    -- ── Contacto adicional ───────────────────────────────────
    telefono2                   VARCHAR(20)     NULL,   -- "Teléf. 2"
    fax                         VARCHAR(20)     NULL,   -- "Fax"

    -- ── Datos legales / identificación ───────────────────────
    nombre_alterno              VARCHAR(255)    NULL,   -- "Nombre Alterno" (razón social completa)
    sucursal                    VARCHAR(20)     NULL,   -- "Sucursal"       ej: PRI
    localizacion                VARCHAR(10)     NULL,   -- "Localización"   ej: L
    regimen_fiscal              VARCHAR(10)     NULL,   -- "Codigo del Regimen Fiscal" (SAT)

    -- ── Comercial ────────────────────────────────────────────
    termino_pago                INT             NULL,   -- "Término pago"   días
    limite_credito              DECIMAL(18,2)   NULL,   -- "Límite Crédito"
    limite_credito2             DECIMAL(18,2)   NULL,   -- "Límite credito 2da moneda"
    codigo_tipo_proveedor       VARCHAR(20)     NULL,   -- "Código Tipo Proveedor" ej: PRO
    codigo_moneda               VARCHAR(10)     NULL,   -- "Cód. Moneda"    ej: MN
    codigo_zona                 VARCHAR(20)     NULL,   -- "Cód. Zona"
    moneda_unica                BIT             NULL,   -- "Moneda Unica"
    proveedor_global            BIT             NULL,   -- "Proveedor Global"
    usar_nombre_alterno         BIT             NULL,   -- "Usar Nombre Alterno en impresión"
    relacionada                 BIT             NULL,   -- "Relacionada"

    -- ── Fiscal / impuestos ───────────────────────────────────
    retenciones                 VARCHAR(100)    NULL,   -- "Retenciones"
    impuestos                   VARCHAR(100)    NULL,   -- "Impuestos"      ej: 0;1;1;1;1;
    grupo_impuestos             VARCHAR(50)     NULL,   -- "Grupo de Impuestos"

    -- ── Contabilidad ─────────────────────────────────────────
    cuenta_contable_pagar       VARCHAR(50)     NULL,   -- "Cuenta Contable x Pagar"
    cuenta_contable_anticipo    VARCHAR(50)     NULL,   -- "Cuenta Contable Anticipo"

    -- ── Bancario adicional ───────────────────────────────────
    cuenta_bancaria2            VARCHAR(50)     NULL,   -- "Cuenta bancaria 2"
    aba_swift                   VARCHAR(50)     NULL,   -- "aba swift"
    beneficiario                VARCHAR(255)    NULL,   -- "Beneficiario"
    codigo_transferencia        VARCHAR(50)     NULL,   -- "Codigo transferencia proveedor"
    codigo_transaccion          VARCHAR(50)     NULL,   -- "Codigo transaccion"
    definible_transferencia1    VARCHAR(100)    NULL,   -- "Definible transferencia1"
    definible_transferencia2    VARCHAR(100)    NULL,   -- "Definible transferencia2"
    definible_transferencia3    VARCHAR(100)    NULL,   -- "Definible transferencia3"

    -- ── Notas ────────────────────────────────────────────────
    memo                        NVARCHAR(MAX)   NULL,   -- "Memo" (observaciones libres)

    -- ── Metadata MBA3 ────────────────────────────────────────
    fecha_creacion_erp          DATE            NULL;   -- "Fecha de Creación del Registro" (MBA3)

-- Índice para búsqueda rápida por código proveedor MBA3
CREATE INDEX IX_productores_codigo_proveedor ON dbo.productores (codigo_proveedor);
CREATE INDEX IX_productores_numero_erp       ON dbo.productores (numero_erp);
