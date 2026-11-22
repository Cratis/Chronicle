// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>A replica set whose lifetime spans benchmark host replacements.</summary>
public sealed class ActivationMongoHost : IAsyncDisposable
{
    readonly IContainer _container = new ContainerBuilder("mongo:8.2")
        .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
        .WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all")
        .WithPortBinding(27017, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
        .Build();

    /// <summary>Gets the retained database endpoint.</summary>
    public string ConnectionString => $"mongodb://localhost:{_container.GetMappedPublicPort(27017)}/?directConnection=true";

    /// <summary>Starts the replica set and waits for a writable primary.</summary>
    /// <returns>The readiness task.</returns>
    public async Task Start()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await _container.StartAsync(deadline.Token);
        var result = await _container.ExecAsync(
            ["mongosh", "--quiet", "--eval", "const r = rs.initiate({_id:'rs0',members:[{_id:0,host:'localhost:27017'}]}); if(r.ok !== 1) quit(1);"], deadline.Token);
        if (result.ExitCode != 0) throw new InvalidOperationException("MongoDB replica-set initiation failed.");
        var database = new MongoClient(ConnectionString).GetDatabase("admin");
        while (true)
        {
            var hello = await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: deadline.Token);
            if (hello.GetValue("isWritablePrimary", false).ToBoolean())
            {
                var build = await database.RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1), cancellationToken: deadline.Token);
                Console.WriteLine($"ACTIVATION_MONGODB_VERSION {build["version"].AsString}");
                return;
            }
            // Infrastructure readiness backoff, never part of a measured operation.
            await Task.Delay(100, deadline.Token);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(90));
}
