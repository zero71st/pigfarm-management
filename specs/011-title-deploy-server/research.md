# Research: Deploy server to Railway Postgres and seed admin

Decision: Use Railway PostgreSQL for production, automatic EF Core startup migrations, and production seeder that fails startup when required secrets are absent.

Rationale:
- Railway provides managed Postgres with `DATABASE_URL` which is compatible with .NET connection strings when parsed.
- Automatic startup migrations reduce operator friction for small teams; combined with fail-fast behavior and CI-run migrations provides safety.
- Refusing to auto-generate admin in production ensures secrets must be provisioned via the platform secret store and avoids accidental exposure.

Alternatives considered:
- CI-only migrations (safer for complex deployments) — rejected for this project to reduce ops friction for single-owner deployments.
- Auto-generate admin in production — rejected for security reasons.

Actionable guidance:
- Ensure `ASPNETCORE_ENVIRONMENT` is set to `Production` on Railway for production deployments.
- Set `DATABASE_URL` on Railway; ensure SSL mode is allowed in connection string builder.
- Provision `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, and `ADMIN_APIKEY` in Railway secrets for production.
