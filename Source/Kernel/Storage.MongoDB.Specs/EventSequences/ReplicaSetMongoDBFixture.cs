// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Provides a shared single-node replica-set MongoDB container for event sequence storage specs that
/// need transactions (multi-document writes go through a session transaction).
/// </summary>
public sealed class ReplicaSetMongoDBFixture : IAsyncLifetime
{
    const int MongoDBPort = 27017;
    static readonly TimeSpan _primaryReadinessTimeout = TimeSpan.FromSeconds(45);

    IContainer? _container;

    /// <summary>
    /// Gets the MongoDB connection string.
    /// </summary>
    public string ConnectionString => $"mongodb://localhost:{_container!.GetMappedPublicPort(MongoDBPort)}/?directConnection=true";

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var image = Environment.GetEnvironmentVariable("CHRONICLE_SPECS_MONGODB_IMAGE") ?? "mongo";
        _container = new ContainerBuilder(image)
            .WithMongoDBKernelCompatibility()
            .WithCommand("/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null")
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort))
            .Build();

        try
        {
            await _container.StartMongoDBWithDiagnostics();
            await WaitForPrimary();
        }
        catch
        {
            await _container.DisposeAsync();
            _container = null;
            throw;
        }
    }

    internal static bool IsReady(BsonDocument hello) =>
        hello.TryGetValue("setName", out var setName) && setName.IsString && setName.AsString == "rs0" &&
        hello.TryGetValue("isWritablePrimary", out var primary) && primary.IsBoolean && primary.AsBoolean;

    async Task WaitForPrimary()
    {
        // Probe the mapped host address used by the specs, not only mongosh inside the container.
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
        settings.ConnectTimeout = TimeSpan.FromSeconds(2);
        var client = new MongoClient(settings);
        await WaitForPrimary(cancellationToken => client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
            new BsonDocument("hello", 1), cancellationToken: cancellationToken));
    }

    internal async Task WaitForPrimary(Func<CancellationToken, Task<BsonDocument>> probe)
    {
        using var timeout = new CancellationTokenSource(_primaryReadinessTimeout);
        BsonDocument? lastHello = null;
        Exception? lastError = null;

        try
        {
            while (true)
            {
                try
                {
                    lastHello = await probe(timeout.Token);
                    if (IsReady(lastHello))
                    {
                        return;
                    }
                }
                catch (Exception error) when (error is MongoException or TimeoutException)
                {
                    // Connecting and electing a primary can fail transiently during container startup.
                    lastError = error;
                }

                // Infrastructure readiness backoff, bounded by the same cancellation deadline.
                await Task.Delay(100, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"MongoDB replica set rs0 did not elect a usable PRIMARY at {ConnectionString} within {_primaryReadinessTimeout.TotalSeconds} seconds. " +
                $"Last hello: {lastHello?.ToJson() ?? "<none>"}. Last MongoDB error: {lastError?.Message ?? "<none>"}.");
        }
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
