using AGTec.Reference.Grpc.Contracts;
using AwesomeAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AGTec.Reference.Grpc.Tests;

/// <summary>
/// Exercises the internal gRPC capability end to end: the Advisory service is hosted
/// in-memory and called over a real gRPC channel using the contract-first generated client.
/// Proves the §7 "gRPC for internal synchronous calls" path, contract and all.
/// </summary>
public class AdvisoryServiceTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData(-5, "Freezing")]
    [InlineData(8, "Cold")]
    [InlineData(21, "Mild")]
    [InlineData(35, "Hot")]
    public async Task GetAdvisory_returns_advice_for_the_conditions(int celsius, string expected)
    {
        using var channel = GrpcChannel.ForAddress(
            factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = factory.Server.CreateHandler() }
        );

        var client = new AdvisoryService.AdvisoryServiceClient(channel);

        var reply = await client.GetAdvisoryAsync(new AdvisoryRequest { Celsius = celsius });

        reply.Advice.Should().Contain(expected);
    }
}
