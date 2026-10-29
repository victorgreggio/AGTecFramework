#!/usr/bin/env bash
#
# create-project.sh — scaffold a multi-tenant application on AGTecFramework.
#
# Generates a layered solution (Domain / Application / Infrastructure / Api) that consumes the
# AGTec.* NuGet packages, wires every AGTec subsystem by configuration (so providers swap per
# environment with no recompile), and ships agent-facing docs:
#   - ai-docs/      a snapshot of the AGTecFramework package docs for AI agents to read
#   - AGENTS.md     instructions that point an agent at ai-docs/ to understand and use AGTec
#
# It asks which implementation to use LOCALLY vs when DEPLOYED for each subsystem and writes
# those choices into appsettings.Development.json (local) and appsettings.json (deployed).
#
# Usage:
#   ./create-project.sh [--name <App.Name>] [--dir <path>] [--feed <nuget-feed>]
#                       [--agtec-version <ver>] [--yes] [--no-build]
#   --yes       accept all defaults without prompting (also implied when stdin is not a TTY)
#   --no-build  skip the final 'dotnet build' of the generated solution

set -euo pipefail

AGTEC_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ---- defaults ---------------------------------------------------------------------------------
APP=""
DEST=""
FEED="$(cd "$AGTEC_ROOT/.." && pwd)/agtec-local-feed"
AGTEC_VERSION="$(grep -oE '<VersionPrefix>[^<]+' "$AGTEC_ROOT/Directory.Build.props" 2>/dev/null | head -1 | sed 's/<VersionPrefix>//')"
AGTEC_VERSION="${AGTEC_VERSION:-0.1.0}"
ASSUME_YES="false"
DO_BUILD="true"

# local vs deployed provider defaults
LOCAL_MSG="Local";         DEP_MSG="AzureServiceBus"
LOCAL_SECRETS="Configuration"; DEP_SECRETS="AzureKeyVault"
LOCAL_STORAGE="InMemory";  DEP_STORAGE="AzureBlob"
LOCAL_SCANNER="None";      DEP_SCANNER="ClamAv"
LOCAL_FLAGS="Configuration"; DEP_FLAGS="AzureAppConfiguration"
INCLUDE_AI="yes"
LOCAL_CHAT="Stub";         DEP_CHAT="AzureOpenAI"
LOCAL_VECTOR="InMemory";   DEP_VECTOR="PgVector"
DIMS="1536"

# ---- arg parsing ------------------------------------------------------------------------------
while [ $# -gt 0 ]; do
  case "$1" in
    --name) APP="$2"; shift 2;;
    --dir) DEST="$2"; shift 2;;
    --feed) FEED="$2"; shift 2;;
    --agtec-version) AGTEC_VERSION="$2"; shift 2;;
    --yes|-y) ASSUME_YES="true"; shift;;
    --no-build) DO_BUILD="false"; shift;;
    -h|--help) grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) echo "Unknown option: $1" >&2; exit 1;;
  esac
done

[ -t 0 ] || ASSUME_YES="true"   # non-interactive when piped

# ---- prompt helpers ---------------------------------------------------------------------------
ask() { # ask VAR "prompt" "default"
  local __var="$1" __prompt="$2" __default="$3" __ans=""
  if [ "$ASSUME_YES" = "true" ]; then printf -v "$__var" '%s' "$__default"; return; fi
  read -r -p "$__prompt [$__default]: " __ans || true
  printf -v "$__var" '%s' "${__ans:-$__default}"
}

ask_choice() { # ask_choice VAR "prompt" "default" "opt1 opt2 ..."
  local __var="$1" __prompt="$2" __default="$3" __opts="$4" __ans=""
  if [ "$ASSUME_YES" = "true" ]; then printf -v "$__var" '%s' "$__default"; return; fi
  while true; do
    read -r -p "$__prompt ($__opts) [$__default]: " __ans || true
    __ans="${__ans:-$__default}"
    for o in $__opts; do [ "$__ans" = "$o" ] && { printf -v "$__var" '%s' "$__ans"; return; }; done
    echo "  please choose one of: $__opts"
  done
}

# ---- gather inputs ----------------------------------------------------------------------------
echo "AGTecFramework project scaffolder"
echo "--------------------------------"
[ -n "$APP" ] || ask APP "Application name (e.g. Acme.Billing)" "Acme.App"
[ -n "$DEST" ] || ask DEST "Target directory" "$(pwd)/$APP"
ask FEED "NuGet feed for AGTec.* packages (folder or URL)" "$FEED"
ask AGTEC_VERSION "AGTecFramework package version" "$AGTEC_VERSION"

echo
echo "Choose implementations — LOCAL (dev) vs DEPLOYED (prod). Enter to accept the default."
ask_choice LOCAL_MSG     "Messaging — local"      "$LOCAL_MSG"     "Local RabbitMq AzureServiceBus"
ask_choice DEP_MSG       "Messaging — deployed"   "$DEP_MSG"       "Local RabbitMq AzureServiceBus"
ask_choice LOCAL_SECRETS "Secrets — local"        "$LOCAL_SECRETS" "Configuration AzureKeyVault Dapr"
ask_choice DEP_SECRETS   "Secrets — deployed"     "$DEP_SECRETS"   "Configuration AzureKeyVault Dapr"
ask_choice LOCAL_STORAGE "Object storage — local" "$LOCAL_STORAGE" "InMemory FileSystem AzureBlob"
ask_choice DEP_STORAGE   "Object storage — deployed" "$DEP_STORAGE" "InMemory FileSystem AzureBlob"
ask_choice LOCAL_SCANNER "Malware scanner — local"   "$LOCAL_SCANNER" "None ClamAv"
ask_choice DEP_SCANNER   "Malware scanner — deployed" "$DEP_SCANNER"  "None ClamAv"
ask_choice LOCAL_FLAGS   "Feature flags — local"    "$LOCAL_FLAGS" "Configuration AzureAppConfiguration"
ask_choice DEP_FLAGS     "Feature flags — deployed" "$DEP_FLAGS"   "Configuration AzureAppConfiguration"
ask_choice INCLUDE_AI    "Include AI/RAG building blocks?" "$INCLUDE_AI" "yes no"
if [ "$INCLUDE_AI" = "yes" ]; then
  ask_choice LOCAL_CHAT   "AI chat — local"        "$LOCAL_CHAT"   "Stub OpenAI AzureOpenAI"
  ask_choice DEP_CHAT     "AI chat — deployed"     "$DEP_CHAT"     "Stub OpenAI AzureOpenAI"
  ask_choice LOCAL_VECTOR "Vector store — local"   "$LOCAL_VECTOR" "InMemory PgVector"
  ask_choice DEP_VECTOR   "Vector store — deployed" "$DEP_VECTOR"  "InMemory PgVector"
fi

if [ -e "$DEST" ] && [ -n "$(ls -A "$DEST" 2>/dev/null)" ]; then
  echo "Error: target directory '$DEST' exists and is not empty." >&2; exit 1
fi

echo
echo "Creating $APP in $DEST ..."
SRC="$DEST/src"
DOM="$SRC/$APP.Domain"; APPL="$SRC/$APP.Application"; INFRA="$SRC/$APP.Infrastructure"; API="$SRC/$APP.Api"
mkdir -p "$DOM" "$APPL" "$INFRA" "$API"

# =============================================================================================
# Solution-level files
# =============================================================================================
cat > "$DEST/Directory.Build.props" <<'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Warnings are errors in CI only, so local iteration stays fast. -->
    <TreatWarningsAsErrors Condition="'$(ContinuousIntegrationBuild)' == 'true'">true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
EOF

cat > "$DEST/Directory.Packages.props" <<'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <!-- AGTecFramework packages (from the configured feed; see nuget.config). -->
  <ItemGroup Label="AGTecFramework">
    <PackageVersion Include="AGTec.Domain" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Application" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Messaging" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.ServiceDefaults" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Api" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Security" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Tenancy" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Platform" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Platform.Azure" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Platform.ClamAv" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Platform.Dapr" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Ai" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Ai.OpenAI" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.VectorData" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.VectorData.PgVector" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Content" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.Content.Local" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.FeatureManagement" Version="__AGTEC_VERSION__" />
    <PackageVersion Include="AGTec.FeatureManagement.Azure" Version="__AGTEC_VERSION__" />
  </ItemGroup>

  <ItemGroup Label="Dependencies">
    <PackageVersion Include="WolverineFx.RuntimeCompilation" Version="6.50.2" />
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="6.50.2" />
    <PackageVersion Include="WolverineFx.AzureServiceBus" Version="6.50.2" />
    <PackageVersion Include="FluentValidation" Version="12.2.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.AI.Abstractions" Version="10.11.0" />
  </ItemGroup>
</Project>
EOF

cat > "$DEST/nuget.config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="agtec" value="__FEED__" />
  </packageSources>
</configuration>
EOF

cat > "$DEST/.gitignore" <<'EOF'
bin/
obj/
*.user
.vs/
local-data/
[Tt]humbs.db
EOF

# =============================================================================================
# Domain
# =============================================================================================
cat > "$DOM/$APP.Domain.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="AGTec.Domain" />
  </ItemGroup>
</Project>
EOF

cat > "$DOM/Note.cs" <<'EOF'
using AGTec.Domain;

namespace __APP__.Domain;

/// <summary>A sample tenant-scoped aggregate. Note: no public setters (enforced by AGTEC0001).</summary>
public sealed class Note : AggregateRoot<Guid>
{
    private Note(Guid id, string text)
        : base(id) => Text = text;

    public string Text { get; private set; }

    public static Note Create(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new Note(Guid.NewGuid(), text);
    }
}
EOF

# =============================================================================================
# Application
# =============================================================================================
cat > "$APPL/$APP.Application.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\__APP__.Domain\__APP__.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="AGTec.Application" />
    <PackageReference Include="AGTec.Messaging" />
    <PackageReference Include="FluentValidation" />
  </ItemGroup>
</Project>
EOF

cat > "$APPL/Notes.cs" <<'EOF'
using __APP__.Domain;
using AGTec.Application;

namespace __APP__.Application.Notes;

/// <summary>A read model returned to callers.</summary>
public sealed record NoteDto(Guid Id, string Text);

/// <summary>Create a note in the caller's tenant. TenantId is supplied by the edge from the
/// resolved <c>ITenantContext</c>, so the application layer stays free of any web dependency.</summary>
public sealed record CreateNote(string TenantId, string Text) : ICommand<Guid>;

/// <summary>List the notes in the caller's tenant.</summary>
public sealed record ListNotes(string TenantId) : IQuery<IReadOnlyList<NoteDto>>;

/// <summary>Tenant-scoped persistence port for notes (adapter lives in Infrastructure).</summary>
public interface INoteStore
{
    Task<Guid> AddAsync(string tenantId, Guid id, string text, CancellationToken ct = default);

    Task<IReadOnlyList<NoteDto>> ListAsync(string tenantId, CancellationToken ct = default);
}

public static class CreateNoteHandler
{
    public static async Task<Guid> Handle(CreateNote command, INoteStore store, CancellationToken ct)
    {
        var note = Note.Create(command.Text);
        await store.AddAsync(command.TenantId, note.Id, note.Text, ct);
        return note.Id;
    }
}

public static class ListNotesHandler
{
    public static Task<IReadOnlyList<NoteDto>> Handle(ListNotes query, INoteStore store, CancellationToken ct) =>
        store.ListAsync(query.TenantId, ct);
}
EOF

# =============================================================================================
# Infrastructure
# =============================================================================================
AI_REFS=""
if [ "$INCLUDE_AI" = "yes" ]; then
AI_REFS='    <PackageReference Include="AGTec.Ai" />
    <PackageReference Include="AGTec.Ai.OpenAI" />
    <PackageReference Include="AGTec.VectorData" />
    <PackageReference Include="AGTec.VectorData.PgVector" />
    <PackageReference Include="AGTec.Content" />
    <PackageReference Include="AGTec.Content.Local" />
    <PackageReference Include="Microsoft.Extensions.AI.Abstractions" />'
fi
cat > "$INFRA/$APP.Infrastructure.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\\__APP__.Application\\__APP__.Application.csproj" />
    <ProjectReference Include="..\\__APP__.Domain\\__APP__.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="AGTec.Tenancy" />
    <PackageReference Include="AGTec.FeatureManagement" />
    <PackageReference Include="AGTec.FeatureManagement.Azure" />
    <PackageReference Include="AGTec.Platform" />
    <PackageReference Include="AGTec.Platform.Azure" />
    <PackageReference Include="AGTec.Platform.ClamAv" />
    <PackageReference Include="AGTec.Platform.Dapr" />
    <PackageReference Include="WolverineFx.RuntimeCompilation" />
    <PackageReference Include="WolverineFx.RabbitMQ" />
    <PackageReference Include="WolverineFx.AzureServiceBus" />
$AI_REFS
  </ItemGroup>
</Project>
EOF

cat > "$INFRA/AppPlatform.cs" <<'EOF'
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AGTec.Application;
using AGTec.FeatureManagement;
using AGTec.FeatureManagement.Azure;
using AGTec.Platform;
using AGTec.Platform.Azure;
using AGTec.Platform.ClamAv;
using AGTec.Platform.Dapr;
using AGTec.Tenancy;
using __APP__.Application.Notes;
using Wolverine;

namespace __APP__.Infrastructure;

/// <summary>
/// Composition root. Every cloud-coupled capability is registered behind a AGTec port and the
/// concrete provider is chosen from configuration (AGTec:*:Provider), so moving between local and
/// deployed is a config change + restart, never a recompile.
/// </summary>
public static class AppPlatform
{
    public static IHostApplicationBuilder AddAppPlatform(this IHostApplicationBuilder builder)
    {
        // Request-scoped ITenantContext from /tenants/{tenantId}/..., validated against the claim.
        builder.Services.AddAGTecTenancy();

        // Feature flags (Microsoft.FeatureManagement) with per-tenant targeting; provider by config.
        if (string.Equals(builder.Configuration["AGTec:FeatureFlags:Provider"], "AzureAppConfiguration", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddAGTecAzureAppConfiguration();
        }

        builder.Services.AddAGTecFeatureManagement();

        AddPlatformServices(builder);
        AppAiExtensions.AddAppAi(builder);

        // Application ports -> adapters. Replace the in-memory store with EF Core/PostgreSQL
        // for relational aggregates, or Marten where a document/event-sourced model fits.
        builder.Services.AddSingleton<INoteStore, InMemoryNoteStore>();

        builder.UseWolverine(options =>
        {
            options.ApplyAGTecDefaults();
            options.Discovery.IncludeAssembly(typeof(CreateNote).Assembly);
            AppMessaging.Configure(options, builder.Configuration);
        });

        return builder;
    }

    /// <summary>Request-pipeline wiring for the platform (e.g. Azure App Configuration refresh when enabled).</summary>
    public static WebApplication UseAppPlatform(this WebApplication app)
    {
        if (string.Equals(app.Configuration["AGTec:FeatureFlags:Provider"], "AzureAppConfiguration", StringComparison.OrdinalIgnoreCase))
        {
            app.UseAGTecAzureAppConfiguration();
        }

        return app;
    }

    private static void AddPlatformServices(IHostApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var secrets = config["AGTec:Secrets:Provider"] ?? "Configuration";
        if (secrets.Equals("AzureKeyVault", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddAzureKeyVaultSecrets(new Uri(config["AGTec:Secrets:VaultUri"]!));
        }
        else if (secrets.Equals("Dapr", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddDaprSecrets(config["AGTec:Secrets:DaprStoreName"] ?? "secretstore");
        }
        else
        {
            builder.Services.AddConfigurationSecrets();
        }

        var storage = config["AGTec:Storage:Provider"] ?? "InMemory";
        if (storage.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddAzureBlobStorage(config.GetConnectionString("blobs") ?? "UseDevelopmentStorage=true");
        }
        else if (storage.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            var root = config["AGTec:Storage:FileSystem:Root"] ?? Path.Combine(AppContext.BaseDirectory, "local-data", "blobs");
            builder.Services.AddFileSystemStorage(root);
        }
        else
        {
            builder.Services.AddInMemoryStorage();
        }

        var scanner = config["AGTec:Storage:Scanner"] ?? "None";
        if (scanner.Equals("ClamAv", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = (config.GetConnectionString("clamav") ?? "clamav:3310").Split(':');
            var port = endpoint.Length > 1 ? int.Parse(endpoint[1], CultureInfo.InvariantCulture) : 3310;
            builder.Services.AddClamAvScanner(endpoint[0], port);
        }
        else
        {
            builder.Services.AddNoOpMalwareScanner();
        }

        builder.Services.ScanObjectStoreForMalware();
    }
}
EOF

cat > "$INFRA/AppMessaging.cs" <<'EOF'
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RabbitMQ;

namespace __APP__.Infrastructure;

/// <summary>
/// Selects the messaging transport from configuration (AGTec:Messaging:Provider). The same binary
/// runs in-memory locally or on a broker when deployed — switching is a config change, not a recompile.
/// </summary>
internal static class AppMessaging
{
    public static void Configure(WolverineOptions options, IConfiguration config)
    {
        var provider = config["AGTec:Messaging:Provider"] ?? "Local";
        switch (provider.ToLowerInvariant())
        {
            case "rabbitmq":
                options.UseRabbitMq(new Uri(config.GetConnectionString("messaging") ?? "amqp://guest:guest@localhost:5672")).AutoProvision();
                break;
            case "azureservicebus":
                options
                    .UseAzureServiceBus(
                        config.GetConnectionString("messaging")
                            ?? throw new InvalidOperationException("ConnectionStrings:messaging is required for the AzureServiceBus provider.")
                    )
                    .AutoProvision();
                break;
            default:
                // In-memory local mediator. Add EF Core/PostgreSQL + a durable outbox for production.
                break;
        }
    }
}
EOF

cat > "$INFRA/InMemoryNoteStore.cs" <<'EOF'
using System.Collections.Concurrent;
using __APP__.Application.Notes;

namespace __APP__.Infrastructure;

/// <summary>A demo tenant-partitioned store. Replace with EF Core/PostgreSQL for relational data,
/// or Marten where a document/event-sourced model fits.</summary>
internal sealed class InMemoryNoteStore : INoteStore
{
    private readonly ConcurrentDictionary<string, List<NoteDto>> _byTenant = new();

    public Task<Guid> AddAsync(string tenantId, Guid id, string text, CancellationToken ct = default)
    {
        var notes = _byTenant.GetOrAdd(tenantId, _ => []);
        lock (notes)
        {
            notes.Add(new NoteDto(id, text));
        }

        return Task.FromResult(id);
    }

    public Task<IReadOnlyList<NoteDto>> ListAsync(string tenantId, CancellationToken ct = default)
    {
        if (!_byTenant.TryGetValue(tenantId, out var notes))
        {
            return Task.FromResult<IReadOnlyList<NoteDto>>([]);
        }

        lock (notes)
        {
            return Task.FromResult<IReadOnlyList<NoteDto>>(notes.ToArray());
        }
    }
}
EOF

# AppAi.cs — real wiring when AI is enabled, otherwise a no-op so AppPlatform always compiles.
if [ "$INCLUDE_AI" = "yes" ]; then
cat > "$INFRA/AppAi.cs" <<'EOF'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AGTec.Ai;
using AGTec.Ai.OpenAI;
using AGTec.Content;
using AGTec.Content.Local;
using AGTec.VectorData;
using AGTec.VectorData.PgVector;

namespace __APP__.Infrastructure;

/// <summary>
/// AI/RAG building blocks on the standard Microsoft.Extensions.AI abstractions, each selected by
/// configuration. Local defaults need no model or cloud; deployed providers swap in by config.
/// </summary>
internal static class AppAiExtensions
{
    public static void AddAppAi(IHostApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var services = builder.Services;

        var dimensions = config.GetValue<int?>("AGTec:Ai:Embeddings:Dimensions") ?? 1536;
        services.AddDeterministicEmbeddings(dimensions);

        var chat = config["AGTec:Ai:Chat:Provider"] ?? "Stub";
        if (chat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOpenAiChatClient(
                config["AGTec:Ai:Chat:Model"] ?? "gpt-4o-mini",
                config["AGTec:Ai:Chat:ApiKey"] ?? throw new InvalidOperationException("AGTec:Ai:Chat:ApiKey is required for the OpenAI chat provider.")
            );
        }
        else if (chat.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAzureOpenAiChatClient(
                new Uri(config["AGTec:Ai:Chat:Endpoint"] ?? throw new InvalidOperationException("AGTec:Ai:Chat:Endpoint is required for the AzureOpenAI chat provider.")),
                config["AGTec:Ai:Chat:Deployment"] ?? "gpt-4o-mini",
                config["AGTec:Ai:Chat:ApiKey"]
            );
        }
        else
        {
            services.AddStubChatClient();
        }

        var vector = config["AGTec:VectorData:Provider"] ?? "InMemory";
        if (vector.Equals("PgVector", StringComparison.OrdinalIgnoreCase))
        {
            services.AddPgVectorStore(
                config.GetConnectionString("vectors") ?? throw new InvalidOperationException("ConnectionStrings:vectors is required for the PgVector provider."),
                dimensions
            );
        }
        else
        {
            services.AddInMemoryVectorStore();
        }

        services.AddLocalTextExtractor();
        services.AddParagraphChunker();
    }
}
EOF
else
cat > "$INFRA/AppAi.cs" <<'EOF'
using Microsoft.Extensions.Hosting;

namespace __APP__.Infrastructure;

/// <summary>AI/RAG building blocks were not enabled for this project. Add the AGTec.Ai / AGTec.VectorData /
/// AGTec.Content packages and wire them here to enable RAG.</summary>
internal static class AppAiExtensions
{
    public static void AddAppAi(IHostApplicationBuilder builder) { }
}
EOF
fi

# =============================================================================================
# Api
# =============================================================================================
cat > "$API/$APP.Api.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\__APP__.Infrastructure\__APP__.Infrastructure.csproj" />
    <ProjectReference Include="..\__APP__.Application\__APP__.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="AGTec.ServiceDefaults" />
    <PackageReference Include="AGTec.Api" />
    <PackageReference Include="AGTec.Security" />
    <PackageReference Include="AGTec.Tenancy" />
  </ItemGroup>
</Project>
EOF

cat > "$API/Program.cs" <<'EOF'
using __APP__.Application.Notes;
using __APP__.Infrastructure;
using Microsoft.FeatureManagement;
using AGTec.Api;
using AGTec.Security;
using AGTec.ServiceDefaults;
using AGTec.Tenancy;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddAGTecServiceDefaults();
builder.AddAGTecJwtBearer(); // OIDC/JWT (Jwt:Authority / Jwt:Audience). Add RequireScope(..) to lock endpoints down.
builder.Services.AddAGTecApiDefaults();
builder.AddAppPlatform();

var app = builder.Build();

app.UseAppPlatform(); // e.g. Azure App Configuration feature-flag refresh when enabled

app.MapAGTecDefaultEndpoints(); // /alive, /health
app.MapAGTecOpenApi(); // /scalar

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAGTecTenancy(); // resolves ITenantContext from the route; must run after routing + auth

app.MapPost(
        "/tenants/{tenantId}/notes",
        async (CreateNoteRequest request, ITenantContext tenant, IMessageBus bus) =>
        {
            var id = await bus.InvokeAsync<Guid>(new CreateNote(tenant.TenantId, request.Text));
            return Results.Created($"/tenants/{tenant.TenantId}/notes/{id}", new { id });
        }
    )
    .WithTags("Notes");

app.MapGet(
        "/tenants/{tenantId}/notes",
        (ITenantContext tenant, IMessageBus bus) => bus.InvokeAsync<IReadOnlyList<NoteDto>>(new ListNotes(tenant.TenantId))
    )
    .WithTags("Notes");

// Feature flags evaluate per tenant (targeting keys off the tenant). Try /tenants/acme/features/SampleFeature.
app.MapGet(
        "/tenants/{tenantId}/features/{name}",
        async (string name, IVariantFeatureManager features, CancellationToken ct) => await features.IsEnabledAsync(name, ct)
    )
    .WithTags("Features");

app.Run();

/// <summary>Request body for creating a note.</summary>
public sealed record CreateNoteRequest(string Text);
EOF

# appsettings: Development = local providers, base = deployed providers.
APPSETTINGS_TEMPLATE='{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "Jwt": { "Authority": "", "Audience": "api" },
  "AGTec": {
    "Messaging": { "Provider": "__MSG__" },
    "Secrets": { "Provider": "__SECRETS__", "VaultUri": "", "DaprStoreName": "secretstore" },
    "Storage": { "Provider": "__STORAGE__", "Scanner": "__SCANNER__", "FileSystem": { "Root": "" } },
    "Ai": {
      "Chat": { "Provider": "__CHAT__", "Model": "gpt-4o-mini", "Deployment": "gpt-4o-mini", "Endpoint": "", "ApiKey": "" },
      "Embeddings": { "Dimensions": __DIMS__ }
    },
    "VectorData": { "Provider": "__VECTOR__" },
    "FeatureFlags": { "Provider": "__FLAGS__", "AppConfig": { "Endpoint": "" } }
  },
  "FeatureManagement": { "SampleFeature": true },
  "ConnectionStrings": { "messaging": "", "blobs": "UseDevelopmentStorage=true", "clamav": "clamav:3310", "vectors": "", "appconfig": "" }
}'

printf '%s\n' "$APPSETTINGS_TEMPLATE" > "$API/appsettings.Development.json"
sed -i -e "s|__MSG__|$LOCAL_MSG|g" -e "s|__SECRETS__|$LOCAL_SECRETS|g" -e "s|__STORAGE__|$LOCAL_STORAGE|g" \
       -e "s|__SCANNER__|$LOCAL_SCANNER|g" -e "s|__CHAT__|$LOCAL_CHAT|g" -e "s|__VECTOR__|$LOCAL_VECTOR|g" \
       -e "s|__FLAGS__|$LOCAL_FLAGS|g" -e "s|__DIMS__|$DIMS|g" "$API/appsettings.Development.json"

printf '%s\n' "$APPSETTINGS_TEMPLATE" > "$API/appsettings.json"
sed -i -e "s|__MSG__|$DEP_MSG|g" -e "s|__SECRETS__|$DEP_SECRETS|g" -e "s|__STORAGE__|$DEP_STORAGE|g" \
       -e "s|__SCANNER__|$DEP_SCANNER|g" -e "s|__CHAT__|$DEP_CHAT|g" -e "s|__VECTOR__|$DEP_VECTOR|g" \
       -e "s|__FLAGS__|$DEP_FLAGS|g" -e "s|__DIMS__|$DIMS|g" "$API/appsettings.json"

# =============================================================================================
# ai-docs: a snapshot of the AGTecFramework docs for AI agents
# =============================================================================================
AIDOCS="$DEST/ai-docs"
mkdir -p "$AIDOCS/packages" "$AIDOCS/adr"
for r in "$AGTEC_ROOT"/src/*/README.md; do
  cp "$r" "$AIDOCS/packages/$(basename "$(dirname "$r")").md"
done
[ -f "$AGTEC_ROOT/docs/AGTecFramework-Design.md" ] && cp "$AGTEC_ROOT/docs/AGTecFramework-Design.md" "$AIDOCS/overview.md" || true
if ls "$AGTEC_ROOT"/docs/adr/*.md >/dev/null 2>&1; then cp "$AGTEC_ROOT"/docs/adr/*.md "$AIDOCS/adr/"; fi

{
  echo "# AGTecFramework — AI reference docs"
  echo
  echo "A snapshot of the AGTecFramework documentation, copied when this project was scaffolded."
  echo "Read these to understand what the framework offers and how to consume it."
  echo
  echo "- [overview.md](overview.md) — framework design and principles"
  echo "- [adr/](adr/) — architecture decision records"
  echo
  echo "## Packages"
  echo
  for f in "$AIDOCS"/packages/*.md; do
    name="$(basename "$f" .md)"
    echo "- [\`$name\`](packages/$(basename "$f"))"
  done
} > "$AIDOCS/INDEX.md"

# =============================================================================================
# AGENTS.md — tells an AI agent how to use AGTecFramework via ai-docs
# =============================================================================================
cat > "$DEST/AGENTS.md" <<'EOF'
# AGENTS.md — working in this repository

This project is built on **AGTecFramework**, an opinionated set of C#/.NET libraries for
multi-tenant microservices (CQRS + DDD + Clean Architecture, ports & adapters, config-driven
providers). If you are an AI agent, read this file first, then the docs in [`ai-docs/`](ai-docs/).

## Understand the framework first

The authoritative reference for the AGTec.* packages lives in **[`ai-docs/`](ai-docs/)**:

- [`ai-docs/INDEX.md`](ai-docs/INDEX.md) — index of every package.
- [`ai-docs/packages/<Name>.md`](ai-docs/packages/) — per-package usage: the public API, a minimal
  example, config keys, and how adapters swap. **Consult the relevant package doc before using it.**
- [`ai-docs/overview.md`](ai-docs/overview.md) — design and principles.
- [`ai-docs/adr/`](ai-docs/adr/) — the decisions behind the framework.

(These are a snapshot taken at scaffold time. The XML docs shipped in each NuGet package give the
same API surface via IntelliSense.)

## Architecture

Clean Architecture, one project per layer under `src/`:

- `__APP__.Domain` — entities/aggregates on `AGTec.Domain`. **No public setters** (analyzer AGTEC0001).
- `__APP__.Application` — CQRS messages + handlers (`AGTec.Application`: `ICommand`/`ICommand<T>`/
  `IQuery<T>`) and ports. **No web or infrastructure dependencies.**
- `__APP__.Infrastructure` — the composition root (`AppPlatform`) and adapters. All cloud-coupled
  capabilities are registered behind AGTec ports and the provider is chosen from configuration.
- `__APP__.Api` — minimal-API edge: `AGTec.ServiceDefaults`, `AGTec.Api`, `AGTec.Security`, and
  `AGTec.Tenancy`.

## Core rules

1. **Multi-tenancy.** Every data path is tenant-scoped. The edge resolves `ITenantContext`
   (`AGTec.Tenancy`) from the `/tenants/{tenantId}/...` route; pass `tenant.TenantId` into commands and
   queries. Never read tenant identity inside the Application layer — keep it web-free.
2. **CQRS.** A feature is a `record` command/query implementing `ICommand`/`ICommand<T>`/`IQuery<T>`
   plus a static handler class with a `Handle(...)` method; the Api calls `bus.InvokeAsync(...)`.
   Handlers are discovered from the Application assembly.
3. **Ports & adapters, config-driven.** To change an implementation, change the matching
   `AGTec:*:Provider` config key — do **not** edit code or add compile-time branches. The composition
   root already wires every provider (`src/__APP__.Infrastructure/AppPlatform.cs`,
   `AppMessaging.cs`, `AppAi.cs`).
4. **Prefer the standard abstraction.** Use `Microsoft.Extensions.AI` (`IChatClient`,
   `IEmbeddingGenerator<string, Embedding<float>>`) and the AGTec ports rather than new bespoke
   interfaces. Add a new adapter behind an existing port instead of a parallel type.

## Add a feature (example)

1. (Optional) add/extend an aggregate in `__APP__.Domain`.
2. In `__APP__.Application`, add a `record X(...) : ICommand<...>` (or `IQuery<...>`) and a static
   `XHandler.Handle(...)`; declare any new port as an interface here.
3. In `__APP__.Infrastructure`, implement the port and register it in `AppPlatform`.
4. In `__APP__.Api`, map an endpoint under `/tenants/{tenantId}/...` that reads `ITenantContext` and
   calls `bus.InvokeAsync(...)`.

## Provider configuration (this project)

Chosen at scaffold time; local lives in `appsettings.Development.json`, deployed in `appsettings.json`.

| Capability | Config key | Local | Deployed |
| --- | --- | --- | --- |
| Messaging | `AGTec:Messaging:Provider` | `__LOCAL_MSG__` | `__DEP_MSG__` |
| Secrets | `AGTec:Secrets:Provider` | `__LOCAL_SECRETS__` | `__DEP_SECRETS__` |
| Object storage | `AGTec:Storage:Provider` | `__LOCAL_STORAGE__` | `__DEP_STORAGE__` |
| Malware scanner | `AGTec:Storage:Scanner` | `__LOCAL_SCANNER__` | `__DEP_SCANNER__` |
| Feature flags | `AGTec:FeatureFlags:Provider` | `__LOCAL_FLAGS__` | `__DEP_FLAGS__` |
| AI chat | `AGTec:Ai:Chat:Provider` | `__LOCAL_CHAT__` | `__DEP_CHAT__` |
| Vector store | `AGTec:VectorData:Provider` | `__LOCAL_VECTOR__` | `__DEP_VECTOR__` |

Deployed secrets/keys (Key Vault URI, broker/DB connection strings, API keys) come from the
environment or a secret store — never hardcode them.

## Commands

```bash
dotnet build                         # build the solution
dotnet run --project src/__APP__.Api # run the API (Scalar UI at /scalar)
dotnet test                          # run tests (add a test project first)
```

Try it (local, anonymous):
```bash
curl -X POST localhost:5xxx/tenants/acme/notes -H 'content-type: application/json' -d '{"text":"hello"}'
curl localhost:5xxx/tenants/acme/notes
```
EOF

# =============================================================================================
# Project README
# =============================================================================================
cat > "$DEST/README.md" <<'EOF'
# __APP__

A multi-tenant application scaffolded on **AGTecFramework**. See [`AGENTS.md`](AGENTS.md) for the
architecture and conventions, and [`ai-docs/`](ai-docs/) for the framework reference.

## Layout

```
src/__APP__.Domain          domain model (AGTec.Domain)
src/__APP__.Application      CQRS commands/queries/handlers + ports (AGTec.Application)
src/__APP__.Infrastructure   composition root + adapters (AppPlatform, config-driven providers)
src/__APP__.Api              minimal-API edge (ServiceDefaults, OpenAPI, Security, Tenancy)
ai-docs/                     AGTecFramework docs for AI agents
```

## Run

```bash
dotnet build
dotnet run --project src/__APP__.Api
```

The sample endpoints are tenant-scoped under `/tenants/{tenantId}/notes`. Providers are chosen by
configuration (local in `appsettings.Development.json`, deployed in `appsettings.json`) — switching
is a config change, not a recompile.
EOF

# =============================================================================================
# Token substitution + solution assembly
# =============================================================================================
find "$DEST" -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.props' -o -name '*.config' -o -name '*.md' -o -name '*.json' \) -print0 \
  | xargs -0 sed -i \
      -e "s|__APP__|$APP|g" \
      -e "s|__AGTEC_VERSION__|$AGTEC_VERSION|g" \
      -e "s|__FEED__|$FEED|g" \
      -e "s|__LOCAL_MSG__|$LOCAL_MSG|g" -e "s|__DEP_MSG__|$DEP_MSG|g" \
      -e "s|__LOCAL_SECRETS__|$LOCAL_SECRETS|g" -e "s|__DEP_SECRETS__|$DEP_SECRETS|g" \
      -e "s|__LOCAL_STORAGE__|$LOCAL_STORAGE|g" -e "s|__DEP_STORAGE__|$DEP_STORAGE|g" \
      -e "s|__LOCAL_SCANNER__|$LOCAL_SCANNER|g" -e "s|__DEP_SCANNER__|$DEP_SCANNER|g" \
      -e "s|__LOCAL_FLAGS__|$LOCAL_FLAGS|g" -e "s|__DEP_FLAGS__|$DEP_FLAGS|g" \
      -e "s|__LOCAL_CHAT__|$LOCAL_CHAT|g" -e "s|__DEP_CHAT__|$DEP_CHAT|g" \
      -e "s|__LOCAL_VECTOR__|$LOCAL_VECTOR|g" -e "s|__DEP_VECTOR__|$DEP_VECTOR|g"

( cd "$DEST" && dotnet new sln -n "$APP" >/dev/null && dotnet sln add src/*/*."csproj" >/dev/null )

echo "Scaffolded $APP at $DEST"

if [ "$DO_BUILD" = "true" ]; then
  echo "Building ..."
  ( cd "$DEST" && dotnet build >/dev/null && echo "Build succeeded." ) || { echo "Build failed — see output above." >&2; exit 1; }
fi

cat <<EOF

Done. Next:
  cd "$DEST"
  dotnet run --project src/$APP.Api
  # Scalar UI at /scalar; sample endpoints under /tenants/{tenantId}/notes
Read AGENTS.md and ai-docs/ to understand and extend the AGTecFramework usage.
EOF
