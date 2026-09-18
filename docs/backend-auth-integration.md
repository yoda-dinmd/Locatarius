# Backend authentication integration (#75)

The API listens on plain HTTP port 8080. TLS terminates at the future nginx
container; HTTPS redirection belongs there. The current Compose stack does not
yet provide that proxy, so its HTTP health check does not prove browser login works.
Secure, HttpOnly, SameSite=Lax and host-only session/CSRF cookies remain required.

## nginx handoff

DevOps must supply `Proxy__TrustedProxy` as the **nginx container IP**, after checking
the home VM's Docker networks for overlap and assigning a stable proxy address.
This is not the developer PC, VM LAN address, browser address, or a whole subnet.
No deployment address is hardcoded in the API. An empty setting ignores forwarded
headers; an invalid nonempty IP fails startup. Only the configured immediate peer
can forward `X-Forwarded-For` and `X-Forwarded-Proto`, with one forwarding hop.

nginx must replace incoming forwarded headers with the actual client address and
`https` scheme, forward `/api/` unchanged to `http://backend:8080`, and redirect HTTP
to HTTPS. DevOps will remove backend/frontend host port mappings when adding nginx
and the frontend container. Do not expose this trust-enabled backend to clients.
Certificates, hostnames and the final Docker network remain DevOps deployment inputs.

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
`schema.sql` is restored as the existing documented schema reference so links build;
reconciling it with EF migrations and the required model remains #89 work.

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
actual nginx, browser certificate trust and the home VM still need end-to-end
verification in #78 after the branches are merged.
