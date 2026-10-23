using AGTec.Tenancy;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.FeatureManagement.Tests;

public class AGTecTenantTargetingContextAccessorTests
{
    [Fact]
    public async Task Maps_the_tenant_to_a_group_and_the_workspace_to_the_user_id()
    {
        var accessor = AccessorFor(new StubTenant(hasTenant: true, tenantId: "acme", workspaceId: "ws-1"));

        var context = await accessor.GetContextAsync();

        context.UserId.Should().Be("ws-1");
        context.Groups.Should().ContainSingle().Which.Should().Be("acme");
    }

    [Fact]
    public async Task Falls_back_to_the_tenant_id_when_no_workspace()
    {
        var accessor = AccessorFor(new StubTenant(hasTenant: true, tenantId: "acme", workspaceId: null));

        (await accessor.GetContextAsync()).UserId.Should().Be("acme");
    }

    [Fact]
    public async Task Returns_an_empty_context_when_no_tenant()
    {
        var accessor = AccessorFor(new StubTenant(hasTenant: false, tenantId: "unused", workspaceId: null));

        var context = await accessor.GetContextAsync();

        context.Groups.Should().BeEmpty();
    }

    private static AGTecTenantTargetingContextAccessor AccessorFor(ITenantContext tenant)
    {
        var services = new ServiceCollection().AddSingleton(tenant).BuildServiceProvider();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { RequestServices = services },
        };
        return new AGTecTenantTargetingContextAccessor(httpContextAccessor);
    }

    private sealed class StubTenant(bool hasTenant, string tenantId, string? workspaceId) : ITenantContext
    {
        public bool HasTenant => hasTenant;
        public string TenantId => tenantId;
        public string? WorkspaceId => workspaceId;
    }
}
