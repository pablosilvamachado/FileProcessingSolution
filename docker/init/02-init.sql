-- ============================================
-- CRIAÇÃO DO BANCO
-- ============================================
DO
$$
BEGIN
   IF NOT EXISTS (
       SELECT FROM pg_database WHERE datname = 'filesdb'
   ) THEN
       PERFORM dblink_exec('dbname=postgres', 'CREATE DATABASE filesdb');
   END IF;
END
$$;

-- ============================================
-- CONECTA NO BANCO
-- ============================================
\connect filesdb;

-- ============================================
-- CRIAÇÃO DA TABELA Files
-- ============================================
CREATE TABLE IF NOT EXISTS "Files" (
    "Id" UUID PRIMARY KEY,
    "FileName" VARCHAR(255) NOT NULL,
    "ContentType" VARCHAR(150) NOT NULL,
    "Size" BIGINT NOT NULL,
    "TempPath" TEXT NOT NULL,
    "FinalPath" TEXT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ProcessedAt" TIMESTAMP WITHOUT TIME ZONE NULL,
    "ErrorMessage" TEXT NULL,
    "RetryCount" INT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_files_status ON "Files" ("Status");
CREATE INDEX IF NOT EXISTS idx_files_createdat ON "Files" ("CreatedAt");
