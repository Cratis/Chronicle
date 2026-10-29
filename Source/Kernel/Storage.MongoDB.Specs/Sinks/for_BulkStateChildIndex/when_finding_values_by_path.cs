// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_finding_values_by_path : an_index_over_a_cache
{
    void Establish()
    {
        Put("a", Doc(("joinKey", "j-a"), ("children", Items(Doc(("childId", "c-1"), ("leaves", Items(Doc(("leafId", "l-1")))))))));
        Put("b", Doc(("joinKey", "j-b"), ("children", Items(Doc(("childId", "c-2"), ("leaves", Items(Doc(("leafId", "l-2")))))))));
    }

    [Fact] void should_find_a_top_level_value() => Find("joinKey", "j-b").ShouldEqual(["b"]);
    [Fact] void should_find_a_child() => Find("children.childId", "c-1").ShouldEqual(["a"]);
    [Fact] void should_find_a_nested_child() => Find("children.leaves.leafId", "l-2").ShouldEqual(["b"]);
    [Fact] void should_miss_a_missing_value() => Find("children.childId", "nope").ShouldBeEmpty();
    [Fact] void should_keep_paths_independent() => Find("joinKey", "c-1").ShouldBeEmpty();
    [Fact] void should_match_the_scan() => Find("children.leaves.leafId", "l-1").ShouldEqual(Scan("children.leaves.leafId", "l-1"));
}
