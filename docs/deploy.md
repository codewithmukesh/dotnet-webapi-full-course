# Deploying the Ticketing API

This works on any host that runs a container behind a reverse proxy: a VPS with Dokploy or Coolify, a PaaS, or a cloud container service. The steps are the same. Only where you click changes.

## What you need

| Piece | Notes |
|-------|-------|
| The image | `ghcr.io/codewithmukesh/dotnet-webapi-full-course:sha-<commit>` from CI. Deploy the `sha-` tag, not `latest`, so you always know which commit is running. |
| PostgreSQL 18 | Managed database or a container with a persistent volume. |
| Redis 8 | Managed or a container. The API still works if Redis is down, just slower (see chapter 11). |
| A reverse proxy with TLS | Traefik, Caddy, nginx or your platform's load balancer. The API itself only speaks HTTP on port 8080. |
| `migrations.sql` | The `migrations-sql` artifact from the CI run of the commit you deploy. |

## 1. Configuration (environment variables)

`__` (double underscore) is how .NET maps environment variables to nested config keys.

| Variable | Example | Required |
|----------|---------|----------|
| `ConnectionStrings__Database` | `Host=db.internal;Port=5432;Database=ticketing;Username=ticketing_app;Password=...` | Yes |
| `ConnectionStrings__Redis` | `redis.internal:6379,password=...` | Yes |
| `Jwt__SigningKey` | 32+ characters, random. Generate with `openssl rand -base64 48` | Yes |
| `Proxy__TrustedNetworks__0` | The address of YOUR reverse proxy, e.g. `10.0.1.5/32` | Yes, behind a proxy |
| `ASPNETCORE_ENVIRONMENT` | `Production` (the image default) | No |

Store the password and the signing key in your platform's secret store, never in git.

The app validates its configuration at startup. A missing connection string or a short signing key stops the container right away with a clear error in the logs. That is on purpose.

## 2. Apply the database migrations

The container does **not** migrate the database on startup. With several replicas they would race each other, and the app's database user should not need rights to change the schema.

Run the CI script with a user that may change the schema:

```bash
psql "host=db.internal dbname=ticketing user=ticketing_admin" -v ON_ERROR_STOP=1 -f migrations.sql
```

The script is idempotent. It skips migrations that are already applied, so running it twice is safe. Run it **before** you roll out the new image.

If you forget: `/health/ready` returns `503` with "N pending migration(s)", so the proxy keeps traffic away from the new container.

## 3. Run the container

- Map nothing to the public internet except the proxy. **Do not publish port 8080.**
- Route your domain through the proxy to the container on port 8080. TLS ends at the proxy.
- Health checks:
  - Liveness (restart if failing): `GET /health/live`
  - Readiness (send traffic only when passing): `GET /health/ready` (checks PostgreSQL, Redis and pending migrations)

## 4. The real client IP (don't skip this)

Behind a proxy, every request arrives from the proxy's address. The rate limiter partitions login attempts by IP, so without this step one attacker's failed logins lock out **every** user.

Set `Proxy__TrustedNetworks__0` to your proxy's address. The API then reads the real client IP from `X-Forwarded-For`, but only on requests that come from that address.

Keep it narrow. If you trust a whole network and port 8080 is reachable from it, anyone can send a fake `X-Forwarded-For` and get a fresh rate-limit bucket on every request.

## 5. Smoke test

```bash
curl -i https://api.example.com/health/ready   # 200 Healthy
curl -i https://api.example.com/api/events      # 200 with JSON
```

Then check the logs. Every request line shows the client IP the API sees. If it shows your proxy's address instead of your own IP, step 4 is not working.

## Rolling out a new version

1. Merge to `main`. CI tests the code, publishes the image and the migration script.
2. Apply that commit's `migrations.sql`.
3. Point the container at the new `sha-` tag.
4. Watch `/health/ready` and the logs.

Migrations must stay backward compatible with the version that is still running (add columns, don't rename or drop them in the same release), because for a moment both versions run against the same database.
