// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_building_a_path_without_dirty_history : an_index_over_a_cache
{
    string[] _found;
    string[] _expected;
    string[] _removed;
    string[] _replaced;

    void Establish()
    {
        cache["inserted"] = Doc(("joinKey", "final"));
        cache["replaced"] = Doc(("joinKey", "old"));
        cache["replaced"] = Doc(("joinKey", "final"));
        cache["mutated"] = Doc(("joinKey", "old"));
        ((IDictionary<string, object?>)cache["mutated"])["joinKey"] = "final";
        cache["removed"] = Doc(("joinKey", "removed"));
        cache.TryRemove("removed", out _);
        _expected = Scan("joinKey", "final");
    }

    void Because()
    {
        _found = Find("joinKey", "final");
        _removed = Find("joinKey", "removed");
        _replaced = Find("joinKey", "old");
    }

    [Fact] void should_match_the_current_cache_scan() => _found.ShouldEqual(_expected);
    [Fact] void should_find_all_current_roots() => _found.Length.ShouldEqual(3);
    [Fact] void should_not_find_removed_roots() => _removed.ShouldBeEmpty();
    [Fact] void should_not_find_replaced_values() => _replaced.ShouldBeEmpty();
}
