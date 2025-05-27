// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a global fixture for the test run that runs the Chronicle development container.
/// </summary>
public class ChronicleDevelopmentFixture : ChronicleFixture
{
    /// <summary>
    /// Gets the name of the Mongo container.
    /// </summary>
    public const string HostName = "chronicle";

    /// <summary>
    /// Initializes a new instance of <see cref="ChronicleMongoDBFixture"/>.
    /// </summary>
    public ChronicleDevelopmentFixture() : base(network =>
    {
        var builder = new ContainerBuilder()
            .WithImage("cratis/chronicle:latest-development")

            // For some reason, this makes the container crash every time
            // .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithEnvironment("Storage__ConnectionDetails", $"mongodb://localhost:{MongoDBPort}")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35000, 35000)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017));
        return builder.Build();
    })
    {
    }
}
