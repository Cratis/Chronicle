// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestingHost;

namespace Aksio.Cratis.Common.Grains;

public interface IOrleansClusterFixture : IDisposable
{
    TestCluster Cluster { get; }
}
