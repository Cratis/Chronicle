// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clustering.Integration.Grains;

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.given;

public class a_cluster_with_serialization_test_grain : Specification
{
    protected ClusteringFixture _fixture;
    protected ISerializationTestGrain _grain;

    void Establish()
    {
        _fixture = new ClusteringFixture();
        _fixture.InitializeAsync().GetAwaiter().GetResult();
        _grain = _fixture.GetGrain<ISerializationTestGrain>("test");
    }

    void Cleanup()
    {
        _fixture.DisposeAsync().GetAwaiter().GetResult();
    }
}
