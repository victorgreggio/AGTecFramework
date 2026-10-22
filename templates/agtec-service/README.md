# AGTecService

A AGTecFramework service (telemetry, health, OIDC/JWT, OpenAPI, Minimal APIs).

## Run

```bash
dotnet run
```

Health probes at `/alive` and `/health`; OpenAPI + Scalar UI at `/scalar` in Development.
Endpoints require the `api` OAuth scope — configure `Jwt:Authority` / `Jwt:Audience`.

## Conventions (inherited from AGTecFramework)

```bash
dotnet tool restore         # CSharpier + Husky.Net (pinned)
dotnet csharpier format .   # format; CI and the pre-commit hook enforce it
```

- **CSharpier** formatting, enforced by a **Husky pre-commit hook** (installed on first
  build) and in CI.
- A **curated analyzer gate** in `.editorconfig`; warnings are errors in CI.
- **Renovate** for dependency updates; **NuGet audit** + **Mend** (SCA/SAST) for security
  (see `SECURITY.md` and `.github/workflows/`).
