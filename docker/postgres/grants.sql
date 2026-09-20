-- Run after every migration, including upgrades. New tables need explicit review
-- here instead of automatically receiving broad runtime access.
BEGIN;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM locatarius_app;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM locatarius_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON
    users, user_contacts, user_credentials, user_role, otp_codes, address,
    buildings, apartments, issues, issue_attachments, sessions
TO locatarius_app;
-- __EFMigrationsHistory intentionally has no runtime grants. No DDL/ownership,
-- TRUNCATE, role-management or sequence permissions are granted.
COMMIT;
