// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.SqlClustering.ObserverStorageRoundtrip;

/// <summary>
/// Runs the released writer against an isolated PostgreSQL store without connecting a current client.
/// </summary>
public sealed class ReleasedSqlKernelFixture : IAsyncDisposable
{
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly SqlClusterDatabase _database;
    readonly IContainer _kernel;
    readonly ReleasedSqlObserverReadyWait _ready;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedSqlKernelFixture"/> class.
    /// </summary>
    public ReleasedSqlKernelFixture()
    {
        _database = new(true, _network);
        _ready = new(_database);
        _kernel = new ContainerBuilder("cratis/chronicle:19.106.2-development-slim")
            .WithNetwork(_network)
            .WithEnvironment("CHRONICLE_HEALTH_PORT", "8080")
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "PostgreSql")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", _database.KernelConnection)
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new HttpsHealthWait(8080)).AddCustomWaitStrategy(_ready))
            .Build();
    }

    /// <summary>
    /// Gets the isolated test database.
    /// </summary>
    public SqlClusterDatabase Database => _database;

    /// <summary>
    /// Gets the persisted old-kernel observer selected by the readiness signal.
    /// </summary>
    public (string ConnectionString, string Identifier, string Owner) Observer => _ready.Observer ?? throw new InvalidOperationException("Old kernel is not ready.");

    /// <summary>
    /// Starts PostgreSQL and the old writer.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task Start()
    {
        await _network.CreateAsync();
        await _database.Start(CancellationToken.None);
        await _kernel.StartAsync();
    }

    /// <summary>
    /// Stops the old writer while the new provider migrates and augments its state.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task StopKernel() => _kernel.StopAsync();

    /// <summary>
    /// Restarts the old writer against the augmented schema.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task RestartKernel() => _kernel.StartAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _kernel.DisposeAsync();
        await _database.DisposeAsync();
        await _network.DisposeAsync();
    }
}
