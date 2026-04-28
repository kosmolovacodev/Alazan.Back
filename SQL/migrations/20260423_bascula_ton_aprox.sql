-- Agregar columna ton_aprox a bascula_recepciones
-- Almacena las toneladas aproximadas declaradas por el productor al momento de llegar
IF COL_LENGTH('dbo.bascula_recepciones', 'ton_aprox') IS NULL
    ALTER TABLE dbo.bascula_recepciones ADD ton_aprox DECIMAL(18,3) NULL;
