-- Locatarius final application schema, PostgreSQL 18.
-- Run once against an empty database. IDs are supplied by the application.
BEGIN;
CREATE TABLE users (
    id uuid PRIMARY KEY,
    email varchar(254) NOT NULL UNIQUE,
    display_name varchar(100) NOT NULL CHECK (char_length(btrim(display_name)) BETWEEN 1 AND 100),
    password_hash text NOT NULL,
    must_change_password boolean NOT NULL DEFAULT true,
    is_active boolean NOT NULL DEFAULT true,
    failed_login_count integer NOT NULL DEFAULT 0 CHECK (failed_login_count >= 0),
    locked_until timestamptz,
    oidc_issuer text,
    oidc_subject text,
    mfa_secret_ciphertext text,
    mfa_enabled boolean NOT NULL DEFAULT false,
    mfa_last_used_step bigint,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (email = lower(btrim(email))),
    CHECK ((oidc_issuer IS NULL) = (oidc_subject IS NULL)),
    CHECK (NOT mfa_enabled OR mfa_secret_ciphertext IS NOT NULL),
    UNIQUE (oidc_issuer, oidc_subject)
);
CREATE TABLE associations (
    id uuid PRIMARY KEY,
    name varchar(100) NOT NULL CHECK (char_length(btrim(name)) BETWEEN 1 AND 100)
);
CREATE TABLE roles (
    id smallint PRIMARY KEY,
    name varchar(20) NOT NULL UNIQUE,
    CHECK ((id = 1 AND name = 'admin') OR (id = 2 AND name = 'resident'))
);
INSERT INTO roles (id, name) VALUES (1, 'admin'), (2, 'resident');
CREATE TABLE memberships (
    association_id uuid NOT NULL REFERENCES associations(id) ON DELETE RESTRICT,
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    role_id smallint NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    is_active boolean NOT NULL DEFAULT true,
    PRIMARY KEY (association_id, user_id)
);
CREATE INDEX memberships_user_idx ON memberships(user_id);
CREATE TABLE sessions (
    token_hash bytea PRIMARY KEY CHECK (octet_length(token_hash) = 32),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    kind varchar(20) NOT NULL CHECK (kind IN ('full', 'password_change', 'mfa_setup', 'mfa_challenge')),
    created_at timestamptz NOT NULL DEFAULT now(),
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    CHECK (expires_at > created_at),
    CHECK (last_seen_at >= created_at)
);
CREATE INDEX sessions_user_idx ON sessions(user_id);
CREATE TABLE buildings (
    id uuid PRIMARY KEY,
    association_id uuid NOT NULL REFERENCES associations(id) ON DELETE RESTRICT,
    name varchar(100) NOT NULL CHECK (char_length(btrim(name)) BETWEEN 1 AND 100),
    address varchar(200) NOT NULL CHECK (char_length(btrim(address)) BETWEEN 1 AND 200),
    UNIQUE (association_id, id)
);
CREATE TABLE units (
    id uuid PRIMARY KEY,
    association_id uuid NOT NULL,
    building_id uuid NOT NULL,
    number varchar(20) NOT NULL CHECK (char_length(btrim(number)) BETWEEN 1 AND 20),
    floor smallint CHECK (floor BETWEEN -5 AND 200),
    FOREIGN KEY (association_id, building_id) REFERENCES buildings(association_id, id) ON DELETE RESTRICT,
    UNIQUE (association_id, id),
    UNIQUE (building_id, number)
);
CREATE TABLE unit_memberships (
    association_id uuid NOT NULL,
    unit_id uuid NOT NULL,
    user_id uuid NOT NULL,
    PRIMARY KEY (association_id, unit_id, user_id),
    FOREIGN KEY (association_id, unit_id) REFERENCES units(association_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (association_id, user_id) REFERENCES memberships(association_id, user_id) ON DELETE RESTRICT
);
CREATE INDEX unit_memberships_user_idx ON unit_memberships(association_id, user_id);
CREATE TABLE tickets (
    id uuid PRIMARY KEY,
    association_id uuid NOT NULL,
    unit_id uuid NOT NULL,
    reporter_user_id uuid NOT NULL,
    title varchar(120) NOT NULL CHECK (char_length(btrim(title)) BETWEEN 5 AND 120),
    description varchar(2000) NOT NULL CHECK (char_length(btrim(description)) BETWEEN 10 AND 2000),
    status varchar(20) NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'in_progress', 'resolved')),
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (association_id, unit_id) REFERENCES units(association_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (association_id, reporter_user_id) REFERENCES memberships(association_id, user_id) ON DELETE RESTRICT,
    UNIQUE (association_id, id)
);
CREATE INDEX tickets_reporter_idx ON tickets(association_id, reporter_user_id, created_at, id);
CREATE INDEX tickets_queue_idx ON tickets(association_id, created_at, id);
CREATE TABLE comments (
    id uuid PRIMARY KEY,
    association_id uuid NOT NULL,
    ticket_id uuid NOT NULL,
    author_user_id uuid NOT NULL,
    body varchar(2000) NOT NULL CHECK (char_length(btrim(body)) BETWEEN 1 AND 2000),
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (association_id, ticket_id) REFERENCES tickets(association_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (association_id, author_user_id) REFERENCES memberships(association_id, user_id) ON DELETE RESTRICT
);
CREATE INDEX comments_ticket_idx ON comments(association_id, ticket_id, created_at, id);
COMMIT;
