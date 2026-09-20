#!/usr/bin/env python3
"""Exercise the real local HTTPS stack. Never print credentials, tokens or cookies."""
import argparse
import http.cookiejar
import json
import os
from pathlib import Path
import ssl
import subprocess
import urllib.error
import urllib.request

root = Path(__file__).resolve().parents[1]
os.chdir(root)
parser = argparse.ArgumentParser()
parser.add_argument('--project', help='Compose project used when starting the stack')
parser.add_argument('--recreate', action='store_true', help='Also verify cookies and proxy DNS after backend/frontend recreation')
args = parser.parse_args()
config = {}
for line in (root / '.env').read_text().splitlines():
    if line.strip() and not line.lstrip().startswith('#'):
        key, _, value = line.partition('=')
        config[key] = value
base = 'https://localhost:' + config.get('HTTPS_PORT', '443')
http_base = 'http://localhost:' + config.get('HTTP_PORT', '80')
context = ssl.create_default_context(cafile=str(root / '.local/ca/rootCA.pem'))
jar = http.cookiejar.CookieJar()
client = urllib.request.build_opener(urllib.request.HTTPSHandler(context=context), urllib.request.HTTPCookieProcessor(jar))
compose = ['docker', 'compose'] + (['-p', args.project] if args.project else [])
def command(*arguments, data=None):
    result = subprocess.run(compose + list(arguments), input=data, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError('Compose verification command failed: ' + ' '.join(arguments[:3]))
    return result.stdout.strip()
def request(path, body=None, headers=None, opener=client):
    payload = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(base + path, data=payload, headers={'Content-Type': 'application/json', **(headers or {})})
    try:
        response = opener.open(req, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, response.headers, response.read()
def check(condition, description):
    if not condition:
        raise AssertionError(description)
    print('PASS:', description)
def cookie_flags(headers, name):
    values = headers.get_all('Set-Cookie') or []
    value = next((c for c in values if c.startswith(name + '=')), '').lower()
    check(bool(value) and all(flag in value for flag in ['secure', 'httponly', 'samesite=lax', 'path=/']) and 'domain=' not in value,
          name + ' has secure host-only cookie attributes')
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *unused):
        return None
try:
    urllib.request.build_opener(NoRedirect()).open(http_base + '/api/auth/csrf', timeout=10)
    raise AssertionError('HTTP did not redirect')
except urllib.error.HTTPError as response:
    check(response.code == 301 and response.headers['Location'] == base + '/api/auth/csrf', 'HTTP redirects to HTTPS preserving the path')
for path in ['/', '/dashboard', '/change-password']:
    status, headers, body = request(path)
    check(status == 200 and b'<div id="root">' in body, 'Trusted HTTPS serves SPA route ' + path)
check(request('/api/not-a-real-endpoint')[0] == 404, 'API errors do not become SPA HTML')
check(command('exec', '-T', 'backend', 'curl', '-fsS', 'http://localhost:8080/health/live') == 'Healthy', 'API process liveness')
check(command('exec', '-T', 'backend', 'curl', '-fsS', 'http://localhost:8080/health/ready') == 'Ready', 'Runtime API can query PostgreSQL')
for service in ['backend', 'frontend']:
    cid = command('ps', '-q', service)
    bindings = subprocess.run(['docker', 'inspect', '--format', '{{json .HostConfig.PortBindings}}', cid], capture_output=True, text=True, check=True).stdout
    check(not json.loads(bindings), service + ' has no host-published port')
# An untrusted peer cannot assert HTTPS directly to the HTTP backend.
code = command('exec', '-T', 'backend', 'curl', '-s', '-o', '/dev/null', '-w', '%{http_code}',
               '-H', 'X-Forwarded-Proto: https', 'http://backend:8080/api/auth/csrf')
check(code == '500', 'Backend ignores spoofed HTTPS from an untrusted peer')
status, headers, data = request('/api/auth/csrf', headers={'X-Forwarded-Proto': 'http', 'X-Forwarded-For': '203.0.113.8'})
check(status == 200, 'nginx replaces spoofed scheme headers; CSRF endpoint succeeds')
cookie_flags(headers, '__Host-locatarius-csrf')
token = json.loads(data)['token']
login = {'email': config['SeedAdmin__Email'], 'password': config['SeedAdmin__Password']}
check(request('/api/auth/login', login)[0] == 403, 'Login without CSRF is rejected')
status, headers, data = request('/api/auth/login', login, {'X-CSRF-TOKEN': token})
check(status == 200 and json.loads(data)['next'] == 'change_password', 'Seeded account receives restricted password-change session')
cookie_flags(headers, '__Host-locatarius')
if args.recreate:
    command('up', '-d', '--no-deps', '--force-recreate', '--wait', '--wait-timeout', '120', 'backend', 'frontend')
    # Docker DNS cache has a five-second TTL, and requests can briefly see old IPs.
    import time
    for attempt in range(15):
        try:
            if request('/')[0] == 200 and request('/api/auth/csrf')[0] == 200:
                break
        except (urllib.error.URLError, TimeoutError):
            pass
        time.sleep(1)
    else:
        raise AssertionError('Proxy did not recover after upstream recreation')
    print('PASS: nginx recovers after backend/frontend recreation')
    # Retain the OLD token intentionally: persistent Data Protection keys must decrypt it.
check(request('/api/auth/logout', {})[0] == 403, 'Logout without CSRF is rejected')
replay_cookie = '; '.join(c.name + '=' + c.value for c in jar)
check(request('/api/auth/logout', {}, {'X-CSRF-TOKEN': token})[0] == 200, 'Logout accepts CSRF, including after recreation')
nojar = urllib.request.build_opener(urllib.request.HTTPSHandler(context=context))
check(request('/api/auth/logout', {}, {'Cookie': replay_cookie, 'X-CSRF-TOKEN': token}, opener=nojar)[0] == 401, 'Logged-out session cannot be replayed')
# Two login requests consumed budget. Changing caller-supplied forwarding headers
# must not create new partitions at nginx's public edge.
for i in range(10 if args.recreate else 8):
    check(request('/api/auth/login', {}, {'X-Forwarded-For': f'203.0.113.{i+10}'})[0] == 403, 'CSRF rejection consumes login budget')
status, headers, _ = request('/api/auth/login', {}, {'X-Forwarded-For': '198.51.100.9'})
check(status == 429 and int(headers['Retry-After']) > 0, 'Spoofed client IP cannot bypass the edge login limiter')
sql = '''SELECT current_user = 'locatarius_app'
AND NOT (SELECT rolsuper OR rolcreatedb OR rolcreaterole FROM pg_roles WHERE rolname = current_user)
AND NOT has_schema_privilege(current_user, 'public', 'CREATE')
AND NOT has_table_privilege(current_user, '"__EFMigrationsHistory"', 'UPDATE')
AND NOT has_table_privilege(current_user, '"__EFMigrationsHistory"', 'SELECT')
AND has_table_privilege(current_user, 'sessions', 'SELECT,INSERT,UPDATE,DELETE');'''
# Feed passwords through stdin to a process environment, never via command arguments.
script = '''read -r PGPASSWORD
export PGPASSWORD
exec psql -h postgres -U locatarius_app -d locatarius_db -At -v ON_ERROR_STOP=1
'''
result = command('exec', '-T', 'postgres', 'sh', '-c', script, data=config['RUNTIME_DB_PASSWORD'] + '\n' + sql)
check(result == 't', 'Runtime role has business DML but no DDL or migration-history access')
result = command('exec', '-T', 'postgres', 'psql', '-U', 'locatarius_admin', '-d', 'locatarius_db', '-Atc',
                 "SELECT NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole FROM pg_roles WHERE rolname='locatarius_migrator'")
check(result == 't', 'Migration role is not a bootstrap superuser')
print('Local infrastructure smoke checks passed. Frontend mock and full application authorization are outside this test.')
