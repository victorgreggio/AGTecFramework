using System.Text;
using AwesomeAssertions;
using Dapr.Client;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using AGTec.Platform;
using AGTec.Platform.Dapr;

namespace AGTec.Platform.Tests;

/// <summary>
/// Spike (ADR-001): proves the Dapr secret-store adapter against a REAL Dapr sidecar
/// (daprd) running a local-file secret store component. Dapr plugs in behind the same
/// <see cref="ISecretStore"/> port as the direct adapters; the backend is chosen in the
/// Dapr component YAML, not in code.
/// </summary>
public sealed class DaprSecretStoreTests : IAsyncLifetime
{
    private const string ComponentYaml = """
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: secretstore
        spec:
          type: secretstores.local.file
          version: v1
          metadata:
            - name: secretsFile
              value: /secrets.json
        """;

    private const string SecretsJson = """{ "db-password": "s3cr3t-value" }""";

    private readonly IContainer _daprd = new ContainerBuilder("daprio/daprd:1.14.4")
        .WithResourceMapping(Encoding.UTF8.GetBytes(ComponentYaml), "/resources/secretstore.yaml")
        .WithResourceMapping(Encoding.UTF8.GetBytes(SecretsJson), "/secrets.json")
        .WithEntrypoint("/daprd")
        .WithCommand(
            "--app-id",
            "spike",
            "--dapr-grpc-port",
            "50001",
            "--resources-path",
            "/resources",
            "--log-level",
            "info"
        )
        .WithPortBinding(50001, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("dapr initialized"))
        .Build();

    private ISecretStore _store = null!;

    public async Task InitializeAsync()
    {
        await _daprd.StartAsync();

        var endpoint = $"http://{_daprd.Hostname}:{_daprd.GetMappedPublicPort(50001)}";
        var client = new DaprClientBuilder().UseGrpcEndpoint(endpoint).Build();
        _store = new DaprSecretStore(client, "secretstore");
    }

    public Task DisposeAsync() => _daprd.DisposeAsync().AsTask();

    [Fact]
    public async Task Reads_a_secret_through_the_dapr_sidecar()
    {
        (await _store.GetSecretAsync("db-password")).Should().Be("s3cr3t-value");
    }

    [Fact]
    public async Task Returns_null_for_a_missing_secret()
    {
        (await _store.GetSecretAsync("nonexistent")).Should().BeNull();
    }
}
