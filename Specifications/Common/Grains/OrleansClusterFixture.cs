// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Orleans.TestingHost;

namespace Aksio.Cratis.Common.Grains;

public class OrleansClusterFixture : IDisposable
{
    public OrleansClusterFixture()
    {
        var builder = new TestClusterBuilder();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public void Dispose() => throw new NotImplementedException();
}
