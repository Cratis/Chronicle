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

    /// <summary>
    /// Gets the MongoDB image the fixture starts.
    /// </summary>
    /// <remarks>
    /// Overridable through the CHRONICLE_MONGODB_IMAGE environment variable because MongoDB refuses to start at all on
    /// some host kernels - a developer machine whose container runtime ships a kernel current MongoDB
    /// rejects cannot otherwise run any integration scenario, and the failure arrives as a container
    /// start timeout that says nothing about why.
    /// </remarks>
    public static string ImageName => Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_IMAGE") ?? "mongo:8.2";

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        // Keep initialization output in the logs and readiness probe, including rs.initiate failures.
        var builder = new ContainerBuilder(ImageName)
            .WithCommand("/bin/sh", "-c", MongoReplicaSetWait.StartupCommand)
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new MongoReplicaSetWait(), strategy => strategy.WithTimeout(MongoReplicaSetWait.StrategyTimeout)));
        return builder.Build();
    }
}
