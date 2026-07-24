// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Provides a shared single-node replica-set MongoDB container for event sequence storage specs that
/// need transactions (multi-document writes go through a session transaction).
/// </summary>
public sealed class ReplicaSetMongoDBFixture : IAsyncLifetime
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
            .WithCommand("/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null")
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
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
