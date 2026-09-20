# Local HTTPS and container runbook (#78)

This runbook covers development on one machine. It does not configure the home VM,
public DNS, Let's Encrypt, certificate renewal or internet exposure. The frontend
is still a mock; the smoke script exercises the real backend independently.

## Prerequisites and first start

Install Docker Engine/Compose v2 (including `--wait` support), Python 3, OpenSSL and
[mkcert](https://github.com/FiloSottile/mkcert#installation). Linux browser trust also
needs NSS tools (on openSUSE: `sudo zypper install mozilla-nss-tools`). .NET 10 and
Node 22 are needed only to run source builds/tests outside Docker.

## Get the code

```sh
git clone https://github.com/yoda-dinmd/Locatarius.git
cd Locatarius
```

Until the #78 PR is merged, run `git switch feat/devops-task-78-new`.
After it is merged, use updated `main`. Confirm `scripts/setup-local.py` exists.
The commands below use a POSIX shell. Windows users must install the CA in the
Windows browser's trust store too if running the stack inside WSL; trusting only
the WSL Linux system does not establish Windows browser trust.

Each developer runs setup on their own machine. Do not copy another developer's
`.env`, `.local/ca` directory or private keys.

From the repository root:

```sh
python3 scripts/setup-local.py
CAROOT="$PWD/.local/ca" mkcert -install
docker compose config --quiet
docker compose up --build --wait --wait-timeout 180
python3 scripts/smoke-local.py --recreate
```

If mkcert was downloaded into this checkout, substitute `.local/bin/mkcert` for
`mkcert` in the trust command. Trust installation can prompt for your OS password;
restart your browser afterward. The setup script preserves existing `.env` and
certificate files. On an older checkout, add the new `MIGRATION_DB_PASSWORD` key
using an independent `openssl rand -hex 32` value. Never print `.env` or resolved
Compose configuration into shared logs.

Setup generates independent random bootstrap, migration, runtime and seed secrets.
It creates a local CA in ignored `.local/ca` and SAN-bearing certificates for
`localhost`, `127.0.0.1` and `::1`. Only the leaf certificate/key are mounted in nginx;
never share the CA private key. To renew, move the old leaf pair aside, rerun setup,
then recreate nginx; preserve the CA to retain browser trust. The CA key stays out
of containers. Other machines do not automatically trust it.

For CI, `python3 scripts/setup-local.py --ci` creates a disposable OpenSSL CA and
leaf certificate without installing host trust. The smoke client verifies chain
and hostname using `.local/ca/rootCA.pem`; it never disables TLS verification.
This test alone does not prove the interactive browser trust store is configured.

## Certificate troubleshooting

If mkcert reports **“The local CA is now installed in the system trust store”**
followed by **“no Firefox and/or Chrome/Chromium security databases found”**, the
system installation succeeded but browser-profile discovery failed. Reinstalling
NSS tools does not fix a profile-location mismatch.

1. Start the intended browser once so it creates its profile, then fully close it.
2. Retry `CAROOT="$PWD/.local/ca" mkcert -install` (or the local binary).
3. If it still fails, find the actual browser certificate database. Firefox's
   `about:support` page shows its Profile Directory. Newer Linux locations can
   include `~/.config/mozilla/firefox/<profile>` and `~/.local/share/pki/nssdb`;
   Flatpak/browser variants may use other locations. The folder must contain
   `cert9.db`; do not create or overwrite a browser database to fix discovery.
4. With the browser closed, import this project's public CA into that existing
   database, replacing the example path with the actual directory:

```sh
certutil -A -d "sql:/absolute/path/to/browser/profile" \
  -n "Locatarius local development CA" -t "C,," \
  -i "$PWD/.local/ca/rootCA.pem"
certutil -V -d "sql:/absolute/path/to/browser/profile" \
  -n "Locatarius local development CA" -u L
```

Restart the browser and open `https://localhost`. Import `rootCA.pem`, never
`rootCA-key.pem`. This fallback uses the same CA as mkcert and does not require
regenerating the server certificate. See [mkcert's trust-store documentation](https://github.com/FiloSottile/mkcert#supported-root-stores).

With the stack running, `curl --head https://localhost/` checks system trust without
an override. If it reports connection refused, check `docker compose ps -a`: that
is a service/port issue, not certificate validation. If it succeeds but the browser
still warns, check browser-specific trust and confirm the exact hostname/port.
Never bypass the warning as the permanent setup solution.

## Services, ports and network

| Service | Responsibility / access |
| --- | --- |
| nginx | Loopback HTTP 80 → HTTPS 443; owns TLS and `/api/` routing |
| frontend | Internal port 80, Vite production files with SPA fallback |
| backend | Internal HTTP 8080, non-root .NET 10; cookie/session API |
| postgres | PostgreSQL 18; optional existing loopback DB-tools port 5432 |
| db-provision | One-shot operator job creates/updates DB roles and transfers old EF ownership |
| db-migrator | One-shot restricted migration owner applies EF migrations and safe seeding |
| db-grants | One-shot operator job reapplies reviewed runtime table grants after migrations |

There are four running services and three successful exited setup jobs. Exited 0
for these jobs is expected. nginx waits for backend DB readiness and frontend
health; backend waits for provisioning, migration and grants to finish successfully.

The explicit bridge defaults to `172.28.0.0/24`, with nginx at `172.28.0.10`.
`NGINX_IP` sets both the static Docker address and `Proxy__TrustedProxy`. Check
`ip route` and existing Docker network subnets before starting; change
`DOCKER_SUBNET` and `NGINX_IP` together if they overlap. This address is the nginx
container, never your PC's LAN address. Multiple simultaneous stacks need distinct
subnets and published ports. `HTTP_PORT`, `HTTPS_PORT` and `POSTGRES_PORT` can be
changed in `.env`; the HTTP redirect incorporates the selected HTTPS port.

Backend/frontend have no host publications. nginx overwrites forwarded client IP
and scheme; the backend accepts one hop only from the configured nginx address.
Docker DNS upstream resolution recovers when backend/frontend addresses change.
`expose` is descriptive, not a firewall: the Docker host and authorized containers
can still access the bridge. This configuration is deliberately loopback-only.

## Daily use and demo

Open **https://localhost** (append the configured port if not 443). The root,
`/dashboard` and `/change-password` load the React shell on direct navigation and
refresh. Mock session state still controls which screen React displays. API paths
never fall through to SPA HTML. The default UI is not proof of server login.

```sh
docker compose ps -a
docker compose logs --tail 100 nginx backend db-migrator db-grants
curl --cacert .local/ca/rootCA.pem https://localhost/api/auth/csrf
python3 scripts/smoke-local.py --recreate
docker compose down
docker compose up --build --wait --wait-timeout 180
```

The manual CSRF response contains a token; do not paste it into reports. Prefer
the smoke script during the demo: it prints only pass/fail descriptions, never
passwords, tokens or cookies. It verifies TLS, redirects, SPA routes, cookie flags,
missing-CSRF denial, restricted login, logout/replay denial, spoofed headers,
rate limiting and DB privileges. `--recreate` also verifies upstream DNS recovery
and acceptance of the pre-recreation CSRF token using persisted Data Protection keys.
The script intentionally exhausts the 10/minute login budget; wait a full minute
before rerunning it or testing another login. Use the unchanged synthetic seed
account; an already changed password will not be reset by startup.

The API has internal `/health/live` for process liveness and `/health/ready` for a
real runtime query against the migrated users table. Compose uses readiness; a
missing table, unavailable database or missing query permission makes it unhealthy.

## Database identities, grants and existing data

The PostgreSQL bootstrap identity `locatarius_admin` is operator-only. Only
`db-provision` and `db-grants` receive its password. `locatarius_migrator` is a
non-superuser owner with schema CREATE and ownership of EF tables; it receives no
role-creation or database-creation authority. `locatarius_app` has DML on the explicit
business-table list in `docker/postgres/grants.sql`, with no table ownership,
schema creation, temporary-table creation, TRUNCATE or migration-history access.
These are database privileges, not substitutes for backend authorization policies.

Future migration tables do **not** automatically receive runtime grants. Review
`grants.sql` with #73's owner when adding a table, then run the normal stack startup.
EF migrations and application seeds remain backend-owned; the jobs execute them.

The provisioner runs on each stack startup, so existing PostgreSQL 18 volumes do
not depend on first-init scripts. It creates missing roles, updates migration/runtime
passwords from protected inputs, transfers public EF tables from the old bootstrap
owner to the migrator and removes old broad default grants. Stop the stack before
changing those passwords; restart with the matching environment. The bootstrap
password must match the existing volume; changing `.env` cannot reset it. Back up
valuable data before owner/privilege changes. Do not share this application database
with unrelated tables: the legacy ownership transition covers its public tables.

`pgdata18` and its `/var/lib/postgresql` mount are retained. Normal `down` preserves
all data. `down --volumes` destroys the selected project's database and Data
Protection key ring and is appropriate only for disposable tests. Keep the same
Compose project name when changing checkouts so you deliberately select the same
volumes. Generated configuration sets `COMPOSE_PROJECT_NAME=locatarius-local`.
To reuse an older project's volume deliberately, set its original project name;
without this setting Compose derives different names from worktree directories.

PostgreSQL 16 data must stay in its old volume. Do not mount it into PostgreSQL 18.
Dump the old instance using compatible PostgreSQL tools, restore into a separate
18 volume, verify counts/constraints and EF history in isolation, then apply this
startup process. Keep the original volume until the restore is accepted. No real
PostgreSQL 16 dataset was provided or migrated by this task.

## Persistence and production boundary

Data Protection keys persist in `dataprotection`, writable only by the non-root API
user. They are protected by local filesystem/volume access, not encrypted at rest
by this implementation. A production deployment needs protected/encrypted key
storage, disks/backups, domain certificates/renewal and an ingress review. nginx
supports TLS 1.2/1.3; HSTS is intentionally a deployment decision, not set on localhost.

Full frontend API/identity integration (#77/#83), association authorization (#73),
and real first-login password change (#87/#88) remain separate application work.
