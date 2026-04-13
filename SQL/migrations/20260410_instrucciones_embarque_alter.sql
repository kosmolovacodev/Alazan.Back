-- ============================================================
-- Parche: Agrega columnas nuevas a instrucciones_embarque
-- Ejecutar si la tabla ya existe con la estructura anterior
-- Fecha: 2026-04-10
-- ============================================================

ALTER TABLE dbo.instrucciones_embarque
    ADD referencia_alazan      NVARCHAR(100)  NULL,
        domicilio              NVARCHAR(300)  NULL,
        broker                 NVARCHAR(200)  NULL,
        precio_unitario        NVARCHAR(50)   NULL,
        fecha_embarque         NVARCHAR(100)  NULL,
        lugar_embarque         NVARCHAR(200)  NULL,
        condiciones_generales  NVARCHAR(MAX)  NULL,
        condiciones_especiales NVARCHAR(MAX)  NULL;

-- Ajustar el default de status_documentacion en filas existentes
UPDATE dbo.instrucciones_embarque
   SET status_documentacion = 'Incompleto'
 WHERE status_documentacion NOT IN ('Incompleto', 'Completo', 'Enviado');
