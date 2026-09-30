CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE address (
    address_id uuid NOT NULL,
    locality character varying(100) NOT NULL,
    district character varying(100) NOT NULL,
    street character varying(200) NOT NULL,
    CONSTRAINT "PK_address" PRIMARY KEY (address_id)
);

CREATE TABLE apartments (
    apartment_id uuid NOT NULL,
    building_id uuid NOT NULL,
    apartment_nr character varying(30) NOT NULL,
    floor integer NOT NULL,
    CONSTRAINT "PK_apartments" PRIMARY KEY (apartment_id)
);

CREATE TABLE users (
    user_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    date_of_birth date,
    apartment_id uuid,
    CONSTRAINT "PK_users" PRIMARY KEY (user_id),
    CONSTRAINT "FK_users_apartments_apartment_id" FOREIGN KEY (apartment_id) REFERENCES apartments (apartment_id) ON DELETE SET NULL
);

CREATE TABLE buildings (
    building_id uuid NOT NULL,
    admin_id uuid NOT NULL,
    building_nr character varying(30) NOT NULL,
    number_of_floors integer NOT NULL,
    address_id uuid NOT NULL,
    CONSTRAINT "PK_buildings" PRIMARY KEY (building_id),
    CONSTRAINT "FK_buildings_address_address_id" FOREIGN KEY (address_id) REFERENCES address (address_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_buildings_users_admin_id" FOREIGN KEY (admin_id) REFERENCES users (user_id) ON DELETE RESTRICT
);

CREATE TABLE otp_codes (
    otp_id uuid NOT NULL,
    user_id uuid NOT NULL,
    code_hash text NOT NULL,
    purpose integer NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    attempts integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_otp_codes" PRIMARY KEY (otp_id),
    CONSTRAINT "FK_otp_codes_users_user_id" FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE user_contacts (
    user_id uuid NOT NULL,
    phone_number character varying(30) NOT NULL,
    CONSTRAINT "PK_user_contacts" PRIMARY KEY (user_id),
    CONSTRAINT "FK_user_contacts_users_user_id" FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE user_credentials (
    user_id uuid NOT NULL,
    email character varying(254) NOT NULL,
    password_hash text NOT NULL,
    must_change_password boolean NOT NULL DEFAULT TRUE,
    password_changed_at timestamp with time zone,
    CONSTRAINT "PK_user_credentials" PRIMARY KEY (user_id),
    CONSTRAINT "FK_user_credentials_users_user_id" FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE user_role (
    user_id uuid NOT NULL,
    role integer NOT NULL,
    CONSTRAINT "PK_user_role" PRIMARY KEY (user_id),
    CONSTRAINT "FK_user_role_users_user_id" FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE issues (
    issue_id uuid NOT NULL,
    reported_by uuid NOT NULL,
    building_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(4000) NOT NULL,
    status integer NOT NULL,
    priority integer NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone,
    resolved_at timestamp with time zone,
    CONSTRAINT "PK_issues" PRIMARY KEY (issue_id),
    CONSTRAINT "FK_issues_buildings_building_id" FOREIGN KEY (building_id) REFERENCES buildings (building_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_issues_users_reported_by" FOREIGN KEY (reported_by) REFERENCES users (user_id) ON DELETE RESTRICT
);

CREATE TABLE issue_attachments (
    attachment_id uuid NOT NULL,
    issue_id uuid NOT NULL,
    uploaded_by uuid NOT NULL,
    file_url character varying(1000) NOT NULL,
    file_type character varying(100) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_issue_attachments" PRIMARY KEY (attachment_id),
    CONSTRAINT "FK_issue_attachments_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES issues (issue_id) ON DELETE CASCADE,
    CONSTRAINT "FK_issue_attachments_users_uploaded_by" FOREIGN KEY (uploaded_by) REFERENCES users (user_id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_apartments_building_id_apartment_nr" ON apartments (building_id, apartment_nr);

CREATE INDEX "IX_buildings_address_id" ON buildings (address_id);

CREATE INDEX "IX_buildings_admin_id" ON buildings (admin_id);

CREATE INDEX "IX_issue_attachments_issue_id" ON issue_attachments (issue_id);

CREATE INDEX "IX_issue_attachments_uploaded_by" ON issue_attachments (uploaded_by);

CREATE INDEX "IX_issues_building_id" ON issues (building_id);

CREATE INDEX "IX_issues_reported_by" ON issues (reported_by);

CREATE INDEX "IX_otp_codes_user_id_purpose" ON otp_codes (user_id, purpose);

CREATE UNIQUE INDEX "IX_user_contacts_phone_number" ON user_contacts (phone_number);

CREATE UNIQUE INDEX "IX_user_credentials_email" ON user_credentials (email);

CREATE INDEX "IX_users_apartment_id" ON users (apartment_id);

ALTER TABLE apartments ADD CONSTRAINT "FK_apartments_buildings_building_id" FOREIGN KEY (building_id) REFERENCES buildings (building_id) ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260915111352_InitialUserDatabase', '10.0.0');

COMMIT;

START TRANSACTION;
ALTER TABLE user_credentials ADD failed_login_attempts integer NOT NULL DEFAULT 0;

ALTER TABLE user_credentials ADD locked_until timestamp with time zone;

CREATE TABLE sessions (
    session_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash character varying(64) NOT NULL,
    session_type integer NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    expires_at timestamp with time zone NOT NULL,
    last_seen_at timestamp with time zone,
    revoked_at timestamp with time zone,
    CONSTRAINT "PK_sessions" PRIMARY KEY (session_id),
    CONSTRAINT "FK_sessions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_sessions_token_hash" ON sessions (token_hash);

CREATE INDEX "IX_sessions_user_id" ON sessions (user_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260917091807_AddSessionsAndLoginLockout', '10.0.0');

COMMIT;
