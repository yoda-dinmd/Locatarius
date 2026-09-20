# Backend authentication integration (#75)

The API listens on internal HTTP 8080. nginx now terminates local HTTPS and
redirects HTTP. Follow the [local HTTPS runbook](local-https.md) for certificates,
ports, startup and tests. Secure, HttpOnly, SameSite=Lax and host-only cookie
attributes are unchanged. React authentication remains a mock under #77.

## nginx contract

Compose assigns nginx `NGINX_IP` (default `172.28.0.10`) inside `DOCKER_SUBNET`
(default `172.28.0.0/24`) and passes that same address as `Proxy__TrustedProxy`.
Check host routes/Docker networks for overlap; this is not a PC or VM LAN address.
Only that immediate peer can forward client IP and scheme, with one forwarding hop.
Empty configuration ignores forwarding; an invalid nonempty IP fails startup.

nginx overwrites client-supplied forwarding headers, preserves `/api/` paths and
forwards to `backend:8080`. Backend/frontend have no host publications. Container
DNS refresh handles upstream recreation. The local key ring persists in a dedicated
volume; production encryption/protection of those keys remains deployment work.

## Session and account integration

Login locks the user row, then credentials, before checking the password and
lockout state. Counter resets and session insertion commit together. Other
credential/account operations, including #87 password change, must follow the
same user → credential → sessions lock order and re-read state after acquiring
locks. PostgreSQL integration tests exercise concurrent failures and a blocked
login observing a committed lockout.

Session lookup does not refresh activity. `RecordAcceptedActivityAsync` refreshes
only a live full session after an accepted authenticated request, without changing
its eight-hour absolute expiry or reviving deleted/revoked sessions. Exactly 30
minutes of inactivity expires it. Restricted sessions retain their five-minute
absolute lifetime. The CSRF endpoint records valid full-session activity; protected
endpoints must integrate this operation after authentication and authorization.

The current global-role model still lacks #73's association memberships and active
account state. #83's protected-request integration and #87's password-change flow
remain dependencies; these changes do not mark the entire authentication story done.
`schema.sql` is generated from the current EF migrations; the [database reference](data-model.md)
documents regeneration and explicitly records the remaining required-model gaps.

## Verification

With .NET 10 and Docker available:

```sh
dotnet build backend/Locatarius.slnx -c Release --warnaserror
dotnet test backend/Locatarius.slnx -c Release --no-build
mkdocs build --strict
```

Tests create disposable PostgreSQL 18 containers. They cover cookies, CSRF,
login/logout replay, rate limiting, bounded chunked request bodies, exact session
expiry, concurrency and proxy-header trust. HTTP tests use ASP.NET TestServer;
the local nginx smoke suite now also verifies the real HTTPS path and recreation.
Browser trust installation needs the developer's OS credentials; home VM deployment
remains outside this local setup.
