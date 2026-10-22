using System.Net;
using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using AGTec.Tenancy;

namespace AGTec.Tenancy.Tests;

/// <summary>
/// Hosts a minimal app with tenant resolution and drives the three paths: anonymous route
/// resolution, claim-matches-route (allowed), and claim-mismatches-route (403).
/// </summary>
public sealed class TenantResolutionTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAGTecTenancy(options => options.ClaimType = "tenant");

        _app = builder.Build();

        // Test auth stub: an X-Tenant-Claim header becomes an authenticated user with that claim.
        _app.Use(
            async (context, next) =>
            {
                var claim = context.Request.Headers["X-Tenant-Claim"].ToString();
                if (!string.IsNullOrEmpty(claim))
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant", claim)], "test"));
                }

                await next();
            }
        );

        _app.UseRouting();
        _app.UseAGTecTenancy();
        _app.MapGet("/tenants/{tenantId}/ping", (ITenantContext tenant) => tenant.TenantId);

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Resolves_tenant_from_route_when_anonymous()
    {
        var response = await _client.GetAsync("/tenants/acme/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("acme");
    }

    [Fact]
    public async Task Allows_when_claim_matches_route()
    {
        var response = await SendAsync("/tenants/acme/ping", tenantClaim: "acme");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rejects_when_claim_mismatches_route()
    {
        var response = await SendAsync("/tenants/acme/ping", tenantClaim: "evil-corp");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string tenantClaim)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Tenant-Claim", tenantClaim);
        return await _client.SendAsync(request);
    }
}
