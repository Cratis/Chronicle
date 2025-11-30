// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clustering.Integration.Grains;

namespace Cratis.Chronicle.Clustering.Integration.for_Serialization;

/// <summary>
/// Tests for serialization of IReadOnlyList types across cluster.
/// </summary>
[Collection(ClusterCollection.Name)]
public class when_serializing_ireadonlylist(ClusteringFixture fixture)
{
    [Fact]
    public async Task should_serialize_string_readonly_list()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        IReadOnlyList<string> expected = new List<string> { "one", "two", "three" }.AsReadOnly();

        var result = await grain.TestIReadOnlyList(expected);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task should_serialize_empty_readonly_list()
    {
        var grain = fixture.GetGrain<ISerializationTestGrain>("test");
        IReadOnlyList<string> expected = Array.Empty<string>();

        var result = await grain.TestIReadOnlyList(expected);

        Assert.Empty(result);
    }
}
