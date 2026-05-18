# NexInvoice Ubuntu VPS Deployment

Target VPS:

- IP: `36.50.135.4`
- SSH user: `root`
- SSH port: `22`
- OS: Ubuntu 22.04
- Deployment: Docker Compose

## 1. Connect To The VPS

```bash
ssh root@36.50.135.4
```

Change the root password immediately after first login:

```bash
passwd
```

## 2. Update The Server

```bash
apt update && apt upgrade -y
```

## 3. Install Docker And Compose Plugin

Install Docker using the official convenience script:

```bash
apt install -y ca-certificates curl gnupg
curl -fsSL https://get.docker.com | sh
systemctl enable docker
systemctl start docker
docker compose version
```

If `docker compose version` is not available, install the plugin:

```bash
apt update
apt install -y docker-compose-plugin
```

## 4. Configure Firewall

```bash
ufw allow OpenSSH
ufw allow 80
ufw allow 443
ufw enable
ufw status
```

## 5. Clone The Repository

```bash
git clone <your-repository-url> nexinvoice
cd nexinvoice
```

## 6. Create The VPS `.env` File

Do not commit `.env`. Create it only on the VPS:

```bash
cp .env.example .env
nano .env
```

Example values to replace:

```env
SA_PASSWORD=<strong-sql-server-password>
JWT_SECRET_KEY=<at-least-32-characters-random-secret>
JWT_ISSUER=NexInvoice
JWT_AUDIENCE=NexInvoiceUsers
REDIS_ENABLED=false
FRONTEND_URL=http://36.50.135.4
VITE_API_BASE_URL=/api/v1
SWAGGER_ENABLED=false
SEED_DEMO_USERS=false
SEED_FAKE_DATA=false
```

Use strong generated values for `SA_PASSWORD` and `JWT_SECRET_KEY`.

## 7. Start Production Containers

Default deployment starts API, frontend, SQL Server, and Nginx. Redis is present but disabled by default.

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

To enable Redis, set `REDIS_ENABLED=true` in `.env` and start with the Redis profile:

```bash
docker compose -f docker-compose.prod.yml --profile redis up -d --build
```

## 8. Check Logs And Health

```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f nginx
```

API health endpoint:

```bash
curl http://36.50.135.4/api/v1/health
```

## 9. EF Core Migration Strategy

The API currently applies EF Core migrations on startup through `InitializeDatabaseAsync()`.

Production defaults:

- `Database__SeedDemoUsers=false`
- `Database__SeedFakeData=false`

That means migrations and system role/permission data can be applied without loading fake business data. If you prefer manual migrations later, remove the startup initializer and run a migration bundle or SDK container command during release.

## 10. Domain Setup

At your DNS provider, create:

- `A` record: `@` -> `36.50.135.4`
- `A` record: `www` -> `36.50.135.4`
- Optional `A` record: `api` -> `36.50.135.4` if you split API and frontend domains later

After DNS is ready, update `.env`:

```env
FRONTEND_URL=https://your-domain.com
VITE_API_BASE_URL=/api/v1
```

Then rebuild the frontend:

```bash
docker compose -f docker-compose.prod.yml up -d --build frontend nginx
```

## 11. SSL With Let's Encrypt

Install Certbot on the VPS:

```bash
apt install -y certbot python3-certbot-nginx
```

If Nginx is running only inside Docker, the simplest path is:

1. Stop the Docker Nginx container temporarily:

```bash
docker compose -f docker-compose.prod.yml stop nginx
```

2. Request a certificate:

```bash
certbot certonly --standalone -d your-domain.com -d www.your-domain.com
```

3. Copy or mount `/etc/letsencrypt` into the Docker Nginx container and add a `listen 443 ssl;` server block in `nginx/nginx.conf`.

4. Restart Nginx:

```bash
docker compose -f docker-compose.prod.yml up -d nginx
```

Alternative production path: install host-level Nginx and Certbot on the VPS, terminate SSL on the host, and proxy traffic to the Docker Nginx or API/frontend containers.

## 12. Useful Operations

Restart:

```bash
docker compose -f docker-compose.prod.yml restart
```

Pull and redeploy:

```bash
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

Stop:

```bash
docker compose -f docker-compose.prod.yml down
```

Stop and remove database volumes only when you intentionally want to delete data:

```bash
docker compose -f docker-compose.prod.yml down -v
```
