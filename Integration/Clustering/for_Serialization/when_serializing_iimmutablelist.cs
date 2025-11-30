// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Clustering.Integration.Grains;

namespace Cratis.Chronicle.Clustering.Integration.for_Serialization;

/// <summary>
/// Tests for serialization of IImmutableList types across cluster.
/// </summary>
[Collection(ClusterCollection.Name)]
public class when_serializing_iimmutablelist(ClusteringFixture fixture)
{
    [Fact]
    public async Task should_serialize_string_immutable_list()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = ImmutableList.Create("one", "two", "three");

        var result = await grain.TestIImmutableList(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_serialize_empty_immutable_list()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = ImmutableList<string>.Empty;

        var result = await grain.TestIImmutableList(expected);

        Assert.Empty(result);
    }
}
