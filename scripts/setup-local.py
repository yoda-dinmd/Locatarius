#!/usr/bin/env python3
"""Create ignored local configuration/certificates without replacing existing secrets."""
import argparse
import os
from pathlib import Path
import secrets
import shutil
import subprocess

root = Path(__file__).resolve().parents[1]
os.chdir(root)
parser = argparse.ArgumentParser()
parser.add_argument('--ci', action='store_true', help='Use an isolated OpenSSL CA; do not install host trust')
args = parser.parse_args()
os.umask(0o077)
config = root / '.env'
if not config.exists():
    config.write_text(''.join(f'{key}={value}\n' for key, value in {
        'COMPOSE_PROJECT_NAME': 'locatarius-local',
        'POSTGRES_PASSWORD': secrets.token_hex(32),
        'MIGRATION_DB_PASSWORD': secrets.token_hex(32),
        'RUNTIME_DB_PASSWORD': secrets.token_hex(32),
        'SeedAdmin__Email': 'admin@example.test',
        'SeedAdmin__Password': secrets.token_hex(24),
        'SeedAdmin__FirstName': 'Local', 'SeedAdmin__LastName': 'Admin',
        'SeedDemoData__Enabled': 'false', 'SeedDemoData__Password': '',
        'HTTP_PORT': '80', 'HTTPS_PORT': '443', 'POSTGRES_PORT': '5432',
        'DOCKER_SUBNET': '172.28.0.0/24', 'NGINX_IP': '172.28.0.10',
    }.items()))
    print('Created .env with independent random secrets (values not displayed).')
else:
    print('Preserved existing .env; compare required keys with .env.example.')
ca = root / '.local/ca'
certs = root / 'nginx/certs'
ca.mkdir(parents=True, exist_ok=True)
certs.mkdir(parents=True, exist_ok=True)
cert = certs / 'fullchain.pem'
key = certs / 'privkey.pem'
if cert.exists() or key.exists():
    if not (cert.exists() and key.exists()):
        raise SystemExit('Incomplete certificate pair: restore or move both files before regenerating.')
    print('Preserved existing certificate pair.')
else:
    if args.ci:
        def openssl(*arguments):
            subprocess.run(['openssl', *map(str, arguments)], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        if not (ca / 'rootCA.pem').exists():
            openssl('req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '30',
                    '-subj', '/CN=Locatarius isolated CI CA',
                    '-addext', 'basicConstraints=critical,CA:TRUE',
                    '-addext', 'keyUsage=critical,keyCertSign,cRLSign', '-keyout', ca / 'rootCA-key.pem', '-out', ca / 'rootCA.pem')
        openssl('req', '-newkey', 'rsa:2048', '-nodes', '-subj', '/CN=localhost', '-keyout', key, '-out', ca / 'server.csr')
        extensions = ca / 'extensions.cnf'
        extensions.write_text('subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n')
        openssl('x509', '-req', '-in', ca / 'server.csr', '-CA', ca / 'rootCA.pem', '-CAkey', ca / 'rootCA-key.pem',
                '-CAcreateserial', '-days', '14', '-extfile', extensions, '-out', cert)
    else:
        mkcert = shutil.which('mkcert') or str(root / '.local/bin/mkcert')
        if not Path(mkcert).is_file():
            raise SystemExit('Install mkcert first: https://github.com/FiloSottile/mkcert#installation')
        subprocess.run([mkcert, '-cert-file', str(cert), '-key-file', str(key), 'localhost', '127.0.0.1', '::1'],
                       env={**os.environ, 'CAROOT': str(ca)}, check=True)
    print('Created localhost certificate; only the leaf certificate/key are mounted into nginx.')
if not args.ci:
    print('Install browser/system trust once, then restart the browser:')
    print('  CAROOT="$PWD/.local/ca" mkcert -install')
    print('If using the locally downloaded binary, substitute .local/bin/mkcert for mkcert.')
print('Next: docker compose up --build --wait --wait-timeout 180')
