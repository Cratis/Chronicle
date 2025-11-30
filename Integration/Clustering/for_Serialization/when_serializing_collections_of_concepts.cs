// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Clustering.Integration.Grains;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Clustering.Integration.for_Serialization;

/// <summary>
/// Tests for serialization of collections containing ConceptAs types across cluster.
/// </summary>
[Collection(ClusterCollection.Name)]
public class when_serializing_collections_of_concepts(ClusteringFixture fixture)
{
    [Fact]
    public async Task should_serialize_ienumerable_of_concept()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = new List<EventStoreName>
        {
            new("Store1"),
            new("Store2"),
            EventStoreName.System
        };

        var result = await grain.TestIEnumerableOfConcept(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_serialize_iimmutablelist_of_concept()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = ImmutableList.Create<EventStoreName>(
            new EventStoreName("Store1"),
            new EventStoreName("Store2"),
            EventStoreName.System);

        var result = await grain.TestIImmutableListOfConcept(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_serialize_ireadonlylist_of_concept()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        IReadOnlyList<EventStoreName> expected = new List<EventStoreName>
        {
            new("Store1"),
            new("Store2"),
            EventStoreName.System
        }.AsReadOnly();

        var result = await grain.TestIReadOnlyListOfConcept(expected);

        Assert.Equal(expected, result);
    }
}
