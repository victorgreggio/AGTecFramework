# AGTec.Analyzers

Roslyn analyzers that enforce AGTecFramework domain conventions at compile time. The package ships as an
analyzer (not a runtime dependency), so its rules surface as build warnings in any project that
references it.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Analyzers
```

It installs as a development-only analyzer: the assembly ships under `analyzers/dotnet/cs`, is not
referenced at runtime, and is not transitive to downstream consumers.

## What it provides

| Diagnostic | Severity | Rule |
|-----------|----------|------|
| `AGTEC0001` | Warning | Entity properties should not have public setters. |

**AGTEC0001** flags public setters on properties of types deriving from `AGTec.Domain.Entity<T>`.
Aggregates and entities should encapsulate state and mutate through intention-revealing methods.
Private/protected setters, `init` accessors, and get-only properties are allowed.

## Usage

Given an entity, AGTEC0001 reports on the public setter:

```csharp
// Before — AGTEC0001: Property 'Name' on entity 'Forecast' has a public setter
public sealed class Forecast : Entity<Guid>
{
    public string Name { get; set; }   // public setter on an entity
}

// After — encapsulate the mutation behind a method; init/private setters are fine
public sealed class Forecast : Entity<Guid>
{
    public string Name { get; private set; }

    public void Rename(string name) => Name = name;
}
```

Configure the severity (or disable the rule) in `.editorconfig`:

```ini
[*.cs]
# none | silent | suggestion | warning | error
dotnet_diagnostic.AGTEC0001.severity = error
```

To suppress a single occurrence, use `#pragma` or a `[SuppressMessage]` attribute:

```csharp
#pragma warning disable AGTEC0001
public string LegacyField { get; set; }
#pragma warning restore AGTEC0001
```

## See also

`AGTec.Domain` — defines `Entity<T>` and the aggregate conventions these analyzers enforce.
