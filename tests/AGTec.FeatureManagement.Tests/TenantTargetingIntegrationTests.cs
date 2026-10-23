using System.Text;
using AGTec.Tenancy;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace AGTec.FeatureManagement.Tests;

/// <summary>
/// Drives a real request pipeline (tenant resolution → feature evaluation) to prove the targeting filter
/// uses AGTec's tenant context: a feature targeted at group "acme" is on for tenant acme, off for others.
/// </summary>
public sealed class TenantTargetingIntegrationTests : IAsyncLifetime
{
    // "BetaFeature" is targeted at the "acme" group; "AlwaysOn" is a plain boolean flag.
    private const string FeatureConfig = """
        {
          "FeatureManagement": {
            "AlwaysOn": true,
            "BetaFeature": {
              "EnabledFor": [
                {
                  "Name": "Targeting",
                  "Parameters": {
                    "Audience": {
                      "Groups": [ { "Name": "acme", "RolloutPercentage": 100 } ],
                      "DefaultRolloutPercentage": 0
                    }
                  }
                }
              ]
            }
          }
        }
        """;

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(FeatureConfig)));

        builder.Services.AddAGTecTenancy();
        builder.Services.AddAGTecFeatureManagement();

        _app = builder.Build();
        _app.UseRouting();
        _app.UseAGTecTenancy();
        _app.MapGet(
            "/tenants/{tenantId}/flags/{name}",
            async (string name, IVariantFeatureManager features, CancellationToken ct) =>
                await features.IsEnabledAsync(name, ct)
        );

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Targeted_feature_is_enabled_for_the_targeted_tenant()
    {
        (await _client.GetStringAsync("/tenants/acme/flags/BetaFeature")).Should().Be("true");
    }

    [Fact]
    public async Task Targeted_feature_is_disabled_for_other_tenants()
    {
        (await _client.GetStringAsync("/tenants/globex/flags/BetaFeature")).Should().Be("false");
    }

    [Fact]
    public async Task Boolean_feature_is_enabled_for_every_tenant()
    {
        (await _client.GetStringAsync("/tenants/globex/flags/AlwaysOn")).Should().Be("true");
    }
}
