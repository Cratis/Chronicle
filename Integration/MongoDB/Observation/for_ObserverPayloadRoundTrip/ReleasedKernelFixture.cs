// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Runs a released kernel against an isolated MongoDB without starting a new client against its old wire API.
/// </summary>
public sealed class ReleasedKernelFixture : IAsyncDisposable
{
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly IContainer _mongoDB;
    readonly IContainer _kernel;
    readonly ReleasedObserverReadyWait _ready;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedKernelFixture"/> class.
    /// </summary>
    public ReleasedKernelFixture()
    {
        _mongoDB = new ContainerBuilder("mongo:8.0")
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "chronicle-test")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "chronicle-test")
            .WithNetwork(_network)
            .WithNetworkAliases("mongodb")
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mongosh", "--quiet", "--eval", "db.adminCommand({ ping: 1 })"))
            .Build();
        _ready = new(() => Client);
        _kernel = new ContainerBuilder("cratis/chronicle:19.106.2-development-slim")
            .WithNetwork(_network)
            .WithEnvironment("CHRONICLE_HEALTH_PORT", "8080")
            .WithEnvironment("Chronicle__Storage__Clusters__Default__Type", "MongoDB")
            .WithEnvironment("Chronicle__Storage__Clusters__Default__ConnectionDetails", "mongodb://chronicle-test:chronicle-test@mongodb:27017/?authSource=admin")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new HttpsHealthWait(8080)).AddCustomWaitStrategy(_ready))
            .Build();
    }

    /// <summary>
    /// Gets a host-side client for this fixture's isolated database.
    /// </summary>
    public IMongoClient Client => new MongoClient(new MongoClientSettings
    {
        Server = new MongoServerAddress(_mongoDB.Hostname, _mongoDB.GetMappedPublicPort(27017)),
        Credential = MongoCredential.CreateCredential("admin", "chronicle-test", "chronicle-test")
    });

    /// <summary>
    /// Gets the persisted old-kernel observer selected by the readiness signal.
    /// </summary>
    public (string Database, string Identifier, EventSequenceId Owner) Observer => _ready.Observer ?? throw new InvalidOperationException("Old kernel is not ready.");

    /// <summary>
    /// Starts the database and released kernel.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task Start()
    {
        await _network.CreateAsync();
        await _mongoDB.StartAsync();
        await _kernel.StartAsync();
    }

    /// <summary>
    /// Stops the old writer while the new provider adds shadow metadata.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task StopKernel() => _kernel.StopAsync();

    /// <summary>
    /// Restarts the old writer against the augmented documents.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task RestartKernel() => _kernel.StartAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _kernel.DisposeAsync();
        await _mongoDB.DisposeAsync();
        await _network.DisposeAsync();
    }
}
