-- Migración: catálogo de calibres para análisis de calidad (en mm)
-- Ejecutar UNA sola vez

CREATE TABLE dbo.cat_calibres_analisis_produccion (
    id     INT IDENTITY(1,1) PRIMARY KEY,
    nombre NVARCHAR(50)  NOT NULL,
    activo BIT           NOT NULL DEFAULT 1
);
GO

-- Valores iniciales: rangos y valores individuales en mm
INSERT INTO dbo.cat_calibres_analisis_produccion (nombre) VALUES
  ('6-7 mm'),
  ('7-8 mm'),
  ('8-9 mm'),
  ('9-10 mm'),
  ('10-11 mm'),
  ('11-12 mm'),
  ('12-13 mm'),
  ('6 mm'),
  ('7 mm'),
  ('8 mm'),
  ('9 mm'),
  ('10 mm'),
  ('11 mm'),
  ('12 mm'),
  ('13 mm'),
  ('14 mm'),
  ('15 mm');
GO
