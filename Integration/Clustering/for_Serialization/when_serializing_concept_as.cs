// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clustering.Integration.Grains;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Clustering.Integration.for_Serialization;

/// <summary>
/// Tests for serialization of ConceptAs types across cluster.
/// </summary>
[Collection(ClusterCollection.Name)]
public class when_serializing_concept_as(ClusteringFixture fixture)
{
    [Fact]
    public async Task should_serialize_and_deserialize_correctly()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = new EventStoreName("TestStore");

        var result = await grain.TestConceptAs(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_handle_system_event_store()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");

        var result = await grain.TestConceptAs(EventStoreName.System);

        Assert.Equal(EventStoreName.System, result);
    }
}
