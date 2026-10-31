// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Runs the released writer against an isolated PostgreSQL store without connecting a current client.
/// </summary>
public sealed class ReleasedSqlKernelFixture : IAsyncDisposable
{
    static readonly TimeSpan _readinessTimeout = TimeSpan.FromMinutes(3);

    readonly INetwork _network = new NetworkBuilder().Build();
    readonly IContainer _kernel;
    readonly ReleasedSqlObserverReadyWait _ready;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedSqlKernelFixture"/> class.
    /// </summary>
    public ReleasedSqlKernelFixture()
    {
        Database = new(true, _network);
        _ready = new(Database);
        _kernel = new ContainerBuilder("cratis/chronicle:19.106.2-development-slim")
            .WithNetwork(_network)
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "PostgreSql")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", Database.KernelConnection)
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql")

            // The released kernel predates the dedicated health port: it serves /health over HTTPS on 35000 only.
            // Every wait is bounded so a readiness mismatch fails the spec instead of hanging the test host.
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new HttpsHealthWait(35000), options => options.WithTimeout(_readinessTimeout))
                .AddCustomWaitStrategy(_ready, options => options.WithTimeout(_readinessTimeout)))
            .Build();
    }

    /// <summary>
    /// Gets the isolated test database.
    /// </summary>
    public SqlClusterDatabase Database { get; }

    /// <summary>
    /// Gets the persisted old-kernel observer selected by the readiness signal.
    /// </summary>
    /// <exception cref="InvalidOperationException">The old kernel is not ready.</exception>
    public (string ConnectionString, string Identifier, string Owner) Observer => _ready.Observer ?? throw new InvalidOperationException("Old kernel is not ready.");

    /// <summary>
    /// Starts PostgreSQL and the old writer.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task Start()
    {
        await _network.CreateAsync();
        await Database.Start(CancellationToken.None);
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
        await Database.DisposeAsync();
        await _network.DisposeAsync();
    }
}
