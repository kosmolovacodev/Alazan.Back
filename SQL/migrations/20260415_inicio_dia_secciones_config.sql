-- ============================================================
-- Tabla de configuración de secciones para Inicio de Día
-- Permite gestionar desde la UI qué secciones existen,
-- su nombre, icono y color sin tocar código.
-- ============================================================

CREATE TABLE dbo.inicio_dia_secciones (
    codigo  NVARCHAR(20)  NOT NULL PRIMARY KEY,
    nombre  NVARCHAR(100) NOT NULL,
    icono   NVARCHAR(50)  NOT NULL DEFAULT 'place',
    color   NVARCHAR(20)  NOT NULL DEFAULT '#607D8B',
    activo  BIT           NOT NULL DEFAULT 1,
    orden   INT           NOT NULL DEFAULT 0
);

INSERT INTO dbo.inicio_dia_secciones (codigo, nombre, icono, color, orden) VALUES
('BASCULA', 'Báscula', 'scale',                  '#1565C0', 1),
('VOLCADO', 'Volcado', 'move_to_inbox',           '#2E7D32', 2),
('PROCESO', 'Proceso', 'precision_manufacturing', '#E65100', 3),
('ALMACEN', 'Almacén', 'warehouse',              '#6A1B9A', 4);

GO
