# VATDesk — Deployment

How the live Railway deployment is actually configured, the two real environment-variable
gotchas hit while setting it up, and what would change for an Azure deployment instead.
See `docs/PLAN.md`'s locked decisions for why Railway over Azure.

## How it's deployed

Railway builds directly from this repo's `Dockerfile` — **there is no docker-compose on
Railway**; `docker-compose.yml` is a local-dev-only convenience (Stage A of this session
re-verified it still boots). The VATDesk service is connected to the GitHub repo
(`cretumarius/VATDesk`, branch `main`) and auto-deploys on every push — confirmed twice:
the initial deploy, and again when a favicon-only commit was pushed and a new deployment
appeared and reached `SUCCESS` within seconds, with no manual trigger.

Two services in the Railway project:

- **VATDesk** — the app, built from `Dockerfile` (multi-stage: Node build → dotnet
  publish → ASP.NET runtime image), single container serving both the API and the built
  React SPA.
- **Postgres** — Railway's managed Postgres template image, reached over Railway's
  private network (`postgres.railway.internal`), never exposed publicly.

## Environment variables

Railway does **not** read `.env` or `docker-compose.yml` — every variable has to be set
directly on the service (Railway dashboard, or `railway variable set`). Two real gotchas
were hit getting this right:

### Gotcha 1: hierarchical config keys need double-underscore env names

ASP.NET Core's configuration system maps an environment variable named `Section__Key` to
the config path `Section:Key`. This app reads its JWT signing key from `Jwt:Key` and its
database connection string from `ConnectionStrings:Default` — so the environment
variables have to be named exactly `Jwt__Key` and `ConnectionStrings__Default` (double
underscore), not `JWT_KEY` or `CONNECTION_STRING` or anything else that merely looks
plausible.

**Why local `docker-compose.yml` masks this entirely**: `.env`/`.env.example` use the
friendly, flat names (`JWT_KEY`, `POSTGRES_USER`, etc.) that a human would guess, and
`docker-compose.yml` does the translation for you:

```yaml
environment:
  Jwt__Key: "${JWT_KEY}"   # .env's JWT_KEY -> the container's actual Jwt__Key
```

Running `docker compose up` locally, you only ever type `JWT_KEY` — the double-underscore
translation happens invisibly in the compose file. Railway skips compose entirely and
runs the `Dockerfile`'s image directly, so that translation never happens. The first
deploy attempt set a variable literally named `JWT_KEY` on the Railway service (matching
`.env.example`'s naming, reasonably) — the app never saw it, since it was looking for
`Jwt__Key`, and `Program.cs`'s fail-fast startup guard threw exactly as designed. The fix
was setting the variable name Railway needs directly: `Jwt__Key`.

### Gotcha 2: the connection string is assembled from Railway reference variables

`ConnectionStrings__Default` is set on the VATDesk service using Railway's cross-service
variable reference syntax, pointing at the Postgres service's own variables rather than
copy-pasting its credentials as a literal string:

```
Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
```

Railway resolves these at deploy time; the actual value the app receives is:

```
Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=<generated>
```

This is standard Npgsql keyword=value format, not a `postgres://` URL — Railway's own
`DATABASE_URL`/`DATABASE_PUBLIC_URL` variables are in URL form and are **not** usable
directly here without reformatting.

No `SSL Mode`/`Trust Server Certificate` clause is present or needed: the connection uses
Railway's private internal network (`postgres.railway.internal`), which isn't exposed
publicly, and the app connects successfully without it. (The original plan for this
document assumed an SSL Mode gotcha would need documenting here — checked against the
live variable and confirmed that didn't happen, so it isn't included as one.)

### Port binding

No `PORT` variable is set. The `Dockerfile` bakes in `ENV ASPNETCORE_URLS=http://+:8080`
and `EXPOSE 8080`; the public domain was created with `railway domain --port 8080`,
matching that exposed port explicitly.

## A third real issue: the service itself was initially broken, not just misconfigured

Worth recording since it cost more time than the variable names: the VATDesk service, as
first created, had **zero service instances** — no GitHub repo or image attached at all,
confirmed via a direct GraphQL query (`serviceInstances: []`) and independently via
`railway link`/`railway service source connect` both reporting "service not found." It
wasn't a crash-loop; it was an empty, unprovisioned service stub that had never deployed
once. The fix was deleting that stub (`railway service delete`) and recreating it
properly from the repo (`railway add --repo cretumarius/VATDesk --branch main --service
VATDesk`), which produced a real service instance that then built and deployed correctly
once the two variables above were set.

## How to redeploy

Push to `main` — Railway's GitHub connection auto-builds and deploys. No manual step
needed; this has been directly observed to work.

To force a redeploy without a code change (e.g. after changing a variable), Railway
already does this automatically: changing any service variable (see below) triggers a
fresh deployment on its own.

## How to rotate the JWT signing key

```bash
railway variable set "Jwt__Key=$(openssl rand -base64 48)" --service VATDesk
```

Setting the variable alone is enough — it triggers an automatic redeploy that picks up
the new key. Rotating the key invalidates every previously-issued token; every logged-in
user will need to sign in again.

## Deploying to Azure instead

The app was written with this in mind — nothing about the ASP.NET Core config or the
security hardening (`docs/SECURITY.md`) is Railway-specific, since Azure App Service also
reads hierarchical config from double-underscore environment variables and also
terminates TLS at its platform edge (the same `UseForwardedHeaders()` reasoning applies
unchanged). What would actually change:

- **Container hosting**: Azure Web App for Containers or Container Apps in place of
  Railway's service — same `Dockerfile`, no changes needed to it.
- **Database**: Azure Database for PostgreSQL (Flexible Server) in place of Railway's
  Postgres service.
- **Configuration**: Azure App Settings in place of Railway variables — same two keys
  (`Jwt__Key`, `ConnectionStrings__Default`), same double-underscore convention, set
  through the Azure Portal or `az webapp config appsettings set` instead of
  `railway variable set`.
- **Networking**: an Azure-provided connection string (add `Ssl Mode=Require;Trust
  Server Certificate=true` if Azure's managed Postgres enforces TLS on its connection,
  which Railway's internal network does not — verify against the actual Azure resource
  rather than assuming).
