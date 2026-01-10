// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a global fixture for the test runs only the MongoDB container.
/// </summary>
public class ChronicleInProcessFixture : ChronicleFixture
{
    /// <summary>
    /// Gets the name of the Mongo container.
    /// </summary>
    public const string HostName = "mongo";

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        var builder = new ContainerBuilder("mongo")
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(MongoDBPort, 27017)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017));
        return builder.Build();
    }
}
