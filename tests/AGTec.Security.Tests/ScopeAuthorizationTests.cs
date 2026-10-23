using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using AGTec.Security;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Tokens;

namespace AGTec.Security.Tests;

/// <summary>
/// Hosts a minimal app with a scope-protected endpoint over the in-memory TestServer and
/// drives it with self-issued JWTs. Verifies the full JWT bearer + scope authorization path:
/// 401 without a token, 403 with the wrong scope, 200 with the required scope.
/// </summary>
public sealed class ScopeAuthorizationTests : IAsyncLifetime
{
    private static readonly SymmetricSecurityKey SigningKey = new(
        Encoding.UTF8.GetBytes("agtec-super-secret-signing-key-of-sufficient-length-01234567890")
    );

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.AddAGTecJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey,
            };
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/protected", () => Results.Ok("ok")).RequireScope("api");

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task No_token_is_unauthorized()
    {
        var response = await _client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wrong_scope_is_forbidden()
    {
        var response = await GetWithTokenAsync(CreateToken("profile email"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Required_scope_is_allowed()
    {
        var response = await GetWithTokenAsync(CreateToken("api offline_access"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> GetWithTokenAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static string CreateToken(string scope)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("scope", scope)]),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            }
        );

        return handler.WriteToken(token);
    }
}
