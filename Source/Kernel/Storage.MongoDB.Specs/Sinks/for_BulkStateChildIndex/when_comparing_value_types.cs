// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_comparing_value_types : an_index_over_a_cache
{
    static readonly Guid _guid = Guid.NewGuid();

    [Fact] void should_match_a_guid_with_its_string_form()
    {
        Put("a", Doc(("joinKey", _guid)));
        Find("joinKey", _guid.ToString()).ShouldEqual(["a"]);
    }

    [Fact] void should_match_a_string_with_a_guid()
    {
        Put("a", Doc(("joinKey", _guid.ToString())));
        Find("joinKey", _guid).ShouldEqual(["a"]);
    }

    [Fact] void should_match_an_int_with_a_long()
    {
        Put("a", Doc(("joinKey", 42)));
        Find("joinKey", 42L).ShouldEqual(["a"]);
    }

    [Fact] void should_match_decimals_with_different_scale()
    {
        Put("a", Doc(("joinKey", 1.0m)));
        Find("joinKey", 1.00m).ShouldEqual(["a"]);
    }

    [Fact] void should_match_the_same_instant_with_different_offsets()
    {
        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Put("a", Doc(("joinKey", instant)));
        Find("joinKey", instant.ToOffset(TimeSpan.FromHours(2))).ShouldEqual(["a"]);
    }

    [Fact] void should_match_positive_and_negative_zero()
    {
        Put("a", Doc(("joinKey", 0.0)));
        Find("joinKey", -0.0).ShouldEqual(["a"]);
    }

    [Fact] void should_match_a_collection_valued_leaf_like_the_scan()
    {
        var leaf = new List<object> { 1, 2 };
        Put("a", Doc(("joinKey", leaf)));
        Find("joinKey", leaf).ShouldEqual(Scan("joinKey", leaf));
        Find("joinKey", leaf).ShouldEqual(["a"]);
    }

    [Fact] void should_not_match_different_values()
    {
        Put("a", Doc(("joinKey", 1.5m)));
        Find("joinKey", 2.5m).ShouldBeEmpty();
    }
}
