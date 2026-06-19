// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Provides a shared MongoDB container for sink integration specs.
/// </summary>
public sealed class MongoDBFixture : IAsyncLifetime
{
    const int MongoDBPort = 27017;

    IContainer? _container;

    /// <summary>
    /// Gets the MongoDB connection string.
    /// </summary>
    public string ConnectionString => $"mongodb://localhost:{_container!.GetMappedPublicPort(MongoDBPort)}/?directConnection=true";

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("mongo")
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'db.adminCommand(\"ping\").ok' | grep -q 1"))
            .Build();

        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
