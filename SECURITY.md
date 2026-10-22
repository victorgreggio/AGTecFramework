# Security

AGTecFramework follows Copperleaf's standard security posture. Vulnerabilities and
license risk are caught at three levels, from fastest/cheapest to deepest.

## 1. NuGet vulnerability audit (every build)

`NuGetAudit` is on by default in .NET 10; a restore fails the build on a known-vulnerable
package (direct **or** transitive) — this is what flagged a transitive `SSH.NET`
advisory during development. In CI it runs as a dedicated step:

```bash
dotnet restore
dotnet list package --vulnerable --include-transitive
```

## 2. Automated dependency updates (Renovate)

`renovate.json` opens grouped PRs for dependency updates on a weekly schedule, with
major bumps gated behind the dependency dashboard and security updates labelled
`security`. Central Package Management (`Directory.Packages.props`) keeps every
version in one place.

## 3. Mend (SCA + SAST) — the org security scan

The authoritative scan is **Mend**, run from GitHub Actions
(`.github/workflows/mend.yml`), weekly and on demand. This mirrors the Core Suite /
`Five0` Mend scan (which runs on Azure Pipelines) using the Mend CLI directly:

- **SCA** (`mend dep`) — resolves the restored dependency graph and reports vulnerable
  components, license violations, and the AI-BOM.
- **SAST** (`mend code`) — static analysis of first-party source.

Credentials come from GitHub secrets (`MEND_URL`, `MEND_EMAIL`, `MEND_USER_KEY`,
`MEND_ORGANIZATION`) — never committed.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Report it through the
internal Copperleaf security channel.
