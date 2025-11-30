// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestingHost;

namespace Cratis.Chronicle.Clustering.Integration;

/// <summary>
/// Fixture for clustering integration tests using Orleans TestCluster.
/// </summary>
public class ClusteringFixture : IAsyncLifetime
{
    TestCluster? _cluster;

    /// <summary>
    /// Gets the test cluster.
    /// </summary>
    public TestCluster Cluster => _cluster!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var builder = new TestClusterBuilder(2);
        builder.AddSiloBuilderConfigurator<ClusteringSiloConfigurator>();
        builder.AddClientBuilderConfigurator<ClusteringClientConfigurator>();
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.StopAllSilosAsync();
            await _cluster.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets a grain from the cluster.
    /// </summary>
    /// <typeparam name="TGrain">Type of grain to get.</typeparam>
    /// <param name="key">Grain key.</param>
    /// <returns>The grain reference.</returns>
    public TGrain GetGrain<TGrain>(string key)
        where TGrain : IGrainWithStringKey
        => _cluster!.GrainFactory.GetGrain<TGrain>(key);
}
