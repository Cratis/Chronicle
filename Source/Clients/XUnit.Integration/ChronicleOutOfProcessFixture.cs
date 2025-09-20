// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a global fixture for the test run that runs the Chronicle development container.
/// </summary>
public class ChronicleOutOfProcessFixture : ChronicleFixture
{
    /// <summary>
    /// Gets the name of the Mongo container.
    /// </summary>
    public const string HostName = "chronicle";

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        var builder = new ContainerBuilder();
        builder = ConfigureImage(builder)

            // For some reason, this makes the container crash every time
            // .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithEnvironment("Storage__ConnectionDetails", $"mongodb://localhost:{MongoDBPort}")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017));

        return builder.Build();
    }

    /// <summary>
    /// Configures the container to use the specified image.
    /// </summary>
    /// <param name="builder"><see cref="ContainerBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="ContainerBuilder"/>.</returns>
    protected virtual ContainerBuilder ConfigureImage(ContainerBuilder builder) =>
        builder.WithImage("cratis/chronicle:latest-development");
}
