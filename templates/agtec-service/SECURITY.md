# Security

This service inherits AGTecFramework's security posture — vulnerabilities and license
risk are caught at three levels.

1. **NuGet vulnerability audit (every build)** — `NuGetAudit` fails the build on a
   known-vulnerable package (direct or transitive); CI also runs
   `dotnet list package --vulnerable --include-transitive`.
2. **Automated dependency updates** — `renovate.json` opens grouped, scheduled update
   PRs, with security updates labelled `security`.
3. **Mend (SCA + SAST)** — `.github/workflows/mend.yml` runs weekly and on demand.
   Credentials come from GitHub secrets (`MEND_URL`, `MEND_EMAIL`, `MEND_USER_KEY`,
   `MEND_ORGANIZATION`).

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Report it through the
private security channel shared by the project maintainers.
