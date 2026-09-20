-- This script runs automatically during PostgreSQL initialization.
-- It creates a restricted 'locatarius_app' role meant for runtime operations.
-- Real passwords should be injected via environment variables if possible, 
-- but since this is an init script, we use a placeholder or read from env in a wrapper.
-- To keep it simple and secure without hardcoded secrets, we rely on the psql runtime substitution.

\set APP_PASSWORD `echo $RUNTIME_DB_PASSWORD`

CREATE ROLE locatarius_app WITH LOGIN PASSWORD :'APP_PASSWORD';

-- Grant connection rights
GRANT CONNECT ON DATABASE locatarius_db TO locatarius_app;

-- Connect to the specific database
\c locatarius_db

-- Grant usage on schema public
GRANT USAGE ON SCHEMA public TO locatarius_app;

-- Grant DML permissions on existing tables (if any, though it might be empty at init)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO locatarius_app;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO locatarius_app;

-- Ensure future tables created by the migrator (locatarius_admin) are accessible to locatarius_app
ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_admin IN SCHEMA public 
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO locatarius_app;

ALTER DEFAULT PRIVILEGES FOR ROLE locatarius_admin IN SCHEMA public 
GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO locatarius_app;

