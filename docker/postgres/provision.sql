-- Operator-only, repeatable provisioning for fresh AND existing PostgreSQL 18 volumes.
-- psql \getenv reads protected environment values without shell interpolation.
\getenv migration_password MIGRATION_DB_PASSWORD
\getenv runtime_password RUNTIME_DB_PASSWORD
BEGIN;
SELECT 'CREATE ROLE locatarius_migrator LOGIN' WHERE NOT EXISTS
    (SELECT FROM pg_roles WHERE rolname = 'locatarius_migrator') \gexec
SELECT 'CREATE ROLE locatarius_app LOGIN' WHERE NOT EXISTS
    (SELECT FROM pg_roles WHERE rolname = 'locatarius_app') \gexec
ALTER ROLE locatarius_migrator WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD :'migration_password';
ALTER ROLE locatarius_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD :'runtime_password';
REVOKE locatarius_admin FROM locatarius_migrator, locatarius_app;
REVOKE locatarius_migrator FROM locatarius_app;
REVOKE CREATE, TEMPORARY ON DATABASE locatarius_db FROM PUBLIC, locatarius_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC, locatarius_app;
GRANT CONNECT ON DATABASE locatarius_db TO locatarius_migrator, locatarius_app;
GRANT USAGE, CREATE ON SCHEMA public TO locatarius_migrator;
GRANT USAGE ON SCHEMA public TO locatarius_app;
-- Transfer the existing EF tables from the old bootstrap owner; preserve all data.
SELECT format('ALTER TABLE public.%I OWNER TO locatarius_migrator', tablename)
FROM pg_tables WHERE schemaname = 'public' AND tableowner = 'locatarius_admin' \gexec
SELECT format('ALTER SEQUENCE public.%I OWNER TO locatarius_migrator', sequencename)
FROM pg_sequences WHERE schemaname = 'public' AND sequenceowner = 'locatarius_admin' \gexec
-- Remove the previous broad future-table policy, including migration bookkeeping.
ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_admin IN SCHEMA public REVOKE ALL ON TABLES FROM locatarius_app;
ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_admin IN SCHEMA public REVOKE ALL ON SEQUENCES FROM locatarius_app;
ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_migrator IN SCHEMA public REVOKE ALL ON TABLES FROM locatarius_app;
ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_migrator IN SCHEMA public REVOKE ALL ON SEQUENCES FROM locatarius_app;
COMMIT;
