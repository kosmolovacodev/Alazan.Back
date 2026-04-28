-- Cuenta de la empresa desde la cual se realizó la transferencia al productor
IF COL_LENGTH('dbo.solicitudes_pago', 'cuenta_origen') IS NULL
    ALTER TABLE dbo.solicitudes_pago ADD cuenta_origen NVARCHAR(100) NULL;
