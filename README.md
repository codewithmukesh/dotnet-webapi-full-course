# .NET 11 Web API Full Course: Event Ticketing API

Source code for the free YouTube course **.NET 11 Web API Full Course: Zero to Production**.

An event ticketing API that does not oversell seats: .NET 11 Minimal APIs, EF Core 11 with PostgreSQL, JWT auth with refresh tokens, HybridCache with Redis, rate limiting, health checks, integration tests with real containers, Docker and CI.

## Prerequisites

- [.NET SDK 11](https://dotnet.microsoft.com/download) (`global.json` pins 11.0.100 or a later feature band)
- [Docker](https://www.docker.com/) (PostgreSQL, Redis and the integration tests run in containers)

## Run it

```bash
docker compose up -d                 # PostgreSQL 18 + Redis 8
dotnet tool restore                  # dotnet-ef
dotnet ef database update --project src/Ticketing.Api
dotnet run --project src/Ticketing.Api
```

Open the API reference (Scalar) at http://localhost:5000/scalar/v1.

Optional, for the pagination chapter: load 1 million events.

```bash
docker compose exec -T postgres psql -U postgres -d ticketing < scripts/seed-1m-events.sql
```

Run the whole stack in containers instead (API image included):

```bash
docker compose --profile app up -d --build   # API on http://localhost:8080
```

## Tests

```bash
dotnet test
```

The integration tests start their own PostgreSQL and Redis containers (Testcontainers), so Docker must be running.

## Chapters

Every chapter has two git tags, so you can jump in anywhere:

```bash
git checkout ch05-start   # code at the start of chapter 5
git checkout ch05-end     # finished chapter 5
```

| # | Chapter |
|---|---------|
| 01 | Project setup, Minimal APIs, Scalar |
| 02 | Configuration, validated options, structured logging |
| 03 | Dependency injection lifetimes |
| 04 | EF Core 11 + PostgreSQL |
| 05 | Queries that scale, and the overselling bug |
| 06 | Validation, Result pattern, ProblemDetails |
| 07 | Pagination, filtering, global error handling |
| 08 | JWT auth, refresh tokens, authorization |
| 09 | Caching with HybridCache + Redis |
| 10 | Integration tests with Testcontainers |
| 11 | Rate limiting and health checks |
| 12 | Docker |
| 13 | CI with GitHub Actions |
| 14 | Deploying to production |

Deployment guide: [docs/deploy.md](docs/deploy.md).

## Go deeper

Want the topics this video leaves out? Take the free [.NET Web API Zero to Hero course](https://codewithmukesh.com/courses/dotnet-webapi-zero-to-hero/).
