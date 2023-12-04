// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestingHost;

namespace Aksio.Cratis.Kernel.Grains;

public class OrleansClusterFixture : IOrleansClusterFixture
{
    public OrleansClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder
            .AddClientBuilderConfigurator<OrleansClientConfigurations>()
            .AddSiloBuilderConfigurator<OrleansClusterConfigurations>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public void Dispose() => Cluster.StopAllSilos();
}
