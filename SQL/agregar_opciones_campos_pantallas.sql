-- Agrega columna opciones a Configuracion_Campos_Pantallas
-- Almacena las opciones separadas por coma para campos de tipo 'seleccionar'
-- Ejemplo: 'Rojo,Verde,Azul'  o  'S,N'

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Configuracion_Campos_Pantallas')
      AND name = 'opciones'
)
BEGIN
    ALTER TABLE dbo.Configuracion_Campos_Pantallas
        ADD opciones NVARCHAR(MAX) NULL;
    PRINT 'Columna opciones agregada correctamente.'
END
ELSE
    PRINT 'La columna opciones ya existe.'
