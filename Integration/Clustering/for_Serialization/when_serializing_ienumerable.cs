// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clustering.Integration.Grains;

namespace Cratis.Chronicle.Clustering.Integration.for_Serialization;

/// <summary>
/// Tests for serialization of IEnumerable types across cluster.
/// </summary>
[Collection(ClusterCollection.Name)]
public class when_serializing_ienumerable(ClusteringFixture fixture)
{
    [Fact]
    public async Task should_serialize_string_enumerable()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = new List<string> { "one", "two", "three" };

        var result = await grain.TestIEnumerable(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_serialize_empty_enumerable()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        var expected = Enumerable.Empty<string>();

        var result = await grain.TestIEnumerable(expected);

        Assert.Empty(result);
    }
}
