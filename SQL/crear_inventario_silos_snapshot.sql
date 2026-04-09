-- ============================================================
-- Tabla: inventario_silos_snapshot
-- Propósito: Resumen diario por silo — calculado por tarea programada.
--   Permite saber al inicio de cada día cuántas toneladas hay por silo
--   sin hacer aggregations pesadas en tiempo real.
-- ============================================================

CREATE TABLE dbo.inventario_silos_snapshot (
    id                  BIGINT IDENTITY(1,1) PRIMARY KEY,

    fecha               DATE            NOT NULL,   -- día del snapshot
    bodega_id           BIGINT          NOT NULL,
    bodega_nombre       NVARCHAR(200)   NOT NULL,
    calibre             NVARCHAR(50)    NULL,
    grano_id            INT             NULL,

    -- Ingresos del día
    recepciones_dia     INT             NOT NULL DEFAULT 0,
    kg_bruto_dia        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    kg_neto_dia         DECIMAL(18,2)   NOT NULL DEFAULT 0,

    -- Acumulado total en el silo (todos los registros SinOC + ConOC)
    recepciones_total   INT             NOT NULL DEFAULT 0,
    kg_bruto_total      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    kg_neto_total       DECIMAL(18,2)   NOT NULL DEFAULT 0,

    -- Columnas calculadas (persistidas para queries rápidas)
    toneladas_brutas_dia    AS (kg_bruto_dia   / 1000.0) PERSISTED,
    toneladas_netas_dia     AS (kg_neto_dia    / 1000.0) PERSISTED,
    toneladas_brutas_total  AS (kg_bruto_total / 1000.0) PERSISTED,
    toneladas_netas_total   AS (kg_neto_total  / 1000.0) PERSISTED,

    -- Desglose por estatus OC
    kg_sin_oc           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    kg_con_oc           DECIMAL(18,2)   NOT NULL DEFAULT 0,

    -- Auditoría
    sede_id             INT             NOT NULL,
    calculado_at        DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT UQ_snapshot_fecha_bodega_sede UNIQUE (fecha, bodega_id, sede_id)
);

CREATE INDEX IX_snapshot_sede_fecha
    ON dbo.inventario_silos_snapshot (sede_id, fecha DESC);

GO

-- ============================================================
-- SP: sp_calcular_snapshot_inventario
-- Recalcula (MERGE) el snapshot del día especificado para una sede.
-- Llamar cada noche vía BackgroundService o SQL Agent Job.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_calcular_snapshot_inventario
    @fecha   DATE = NULL,   -- NULL = hoy
    @sedeId  INT  = 0       -- 0 = todas las sedes
AS
BEGIN
    SET NOCOUNT ON;
    IF @fecha IS NULL SET @fecha = CAST(GETDATE() AS DATE);

    MERGE dbo.inventario_silos_snapshot AS t
    USING (
        SELECT
            CAST(fecha_ingreso AS DATE)                         AS fecha,
            bodega_id,
            MAX(bodega_nombre)                                  AS bodega_nombre,
            MAX(calibre)                                        AS calibre,
            MAX(grano_id)                                       AS grano_id,
            -- Ingresos del día
            COUNT(CASE WHEN CAST(fecha_ingreso AS DATE) = @fecha THEN 1 END) AS recepciones_dia,
            SUM(CASE  WHEN CAST(fecha_ingreso AS DATE) = @fecha THEN kg_bruto ELSE 0 END) AS kg_bruto_dia,
            SUM(CASE  WHEN CAST(fecha_ingreso AS DATE) = @fecha THEN kg_neto  ELSE 0 END) AS kg_neto_dia,
            -- Acumulado total
            COUNT(*)                                            AS recepciones_total,
            SUM(kg_bruto)                                       AS kg_bruto_total,
            SUM(kg_neto)                                        AS kg_neto_total,
            -- Desglose OC
            SUM(CASE WHEN status = 'SinOC' THEN kg_neto ELSE 0 END) AS kg_sin_oc,
            SUM(CASE WHEN status = 'ConOC' THEN kg_neto ELSE 0 END) AS kg_con_oc,
            sede_id
        FROM dbo.inventario_silos
        WHERE (@sedeId = 0 OR sede_id = @sedeId)
        GROUP BY CAST(fecha_ingreso AS DATE), bodega_id, sede_id
    ) AS s ON t.fecha = @fecha AND t.bodega_id = s.bodega_id AND t.sede_id = s.sede_id

    WHEN MATCHED THEN UPDATE SET
        bodega_nombre       = s.bodega_nombre,
        calibre             = s.calibre,
        grano_id            = s.grano_id,
        recepciones_dia     = s.recepciones_dia,
        kg_bruto_dia        = s.kg_bruto_dia,
        kg_neto_dia         = s.kg_neto_dia,
        recepciones_total   = s.recepciones_total,
        kg_bruto_total      = s.kg_bruto_total,
        kg_neto_total       = s.kg_neto_total,
        kg_sin_oc           = s.kg_sin_oc,
        kg_con_oc           = s.kg_con_oc,
        calculado_at        = SYSDATETIMEOFFSET()

    WHEN NOT MATCHED THEN INSERT (
        fecha, bodega_id, bodega_nombre, calibre, grano_id,
        recepciones_dia, kg_bruto_dia, kg_neto_dia,
        recepciones_total, kg_bruto_total, kg_neto_total,
        kg_sin_oc, kg_con_oc,
        sede_id, calculado_at
    ) VALUES (
        @fecha, s.bodega_id, s.bodega_nombre, s.calibre, s.grano_id,
        s.recepciones_dia, s.kg_bruto_dia, s.kg_neto_dia,
        s.recepciones_total, s.kg_bruto_total, s.kg_neto_total,
        s.kg_sin_oc, s.kg_con_oc,
        s.sede_id, SYSDATETIMEOFFSET()
    );
END;
GO
