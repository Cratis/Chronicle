// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Fixture that uses the locally built Docker image for CLI integration tests.
/// </summary>
/// <remarks>
/// <para>
/// The CLI connects to the Chronicle server externally via gRPC. The management
/// port is mapped to host port 8081 (instead of the default 8080) to avoid
/// conflicts with a locally running Chronicle instance used during development.
/// Tests pass <c>--management-port 8081</c> to the CLI so that the
/// <c>OAuthTokenProvider</c> reaches <c>http://localhost:8081/connect/token</c>.
/// </para>
/// <para>
/// TLS is not configured. These tests are intended to run with a Debug-built
/// Docker image where the <c>DEVELOPMENT</c> compilation constant is defined,
/// so the server does not require TLS or encryption certificates.
/// </para>
/// </remarks>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        var waitStrategy = Wait.ForUnixContainer()
            .UntilInternalTcpPortIsAvailable(27017)
            .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health"));

        var builder = new ContainerBuilder("cratis/chronicle:latest-development");
        builder = ConfigureImage(builder)
            .WithEnvironment("Storage__ConnectionDetails", $"mongodb://localhost:{MongoDBPort}")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(waitStrategy)
            .WithStartupCallback((container, ct) =>
            {
                Console.WriteLine($"Chronicle container {container.Id} started successfully");
                return Task.CompletedTask;
            });

        return builder.Build();
    }

    /// <inheritdoc/>
    protected override ContainerBuilder ConfigureImage(ContainerBuilder builder) =>
        builder.WithImage(Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development");
}
