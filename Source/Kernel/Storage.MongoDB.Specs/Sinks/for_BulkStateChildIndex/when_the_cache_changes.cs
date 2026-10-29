// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_the_cache_changes : an_index_over_a_cache
{
    [Fact] void should_not_find_a_child_after_it_is_removed()
    {
        Put("a", Doc(("children", Items(Doc(("childId", "c-1"))))));
        Find("children.childId", "c-1").ShouldEqual(["a"]);
        Put("a", Doc(("children", Items())));
        Find("children.childId", "c-1").ShouldBeEmpty();
    }

    [Fact] void should_follow_a_child_moved_between_roots()
    {
        Put("a", Doc(("children", Items(Doc(("childId", "c-1"))))));
        Put("b", Doc(("children", Items())));
        Find("children.childId", "c-1").ShouldEqual(["a"]);
        Put("a", Doc(("children", Items())));
        Put("b", Doc(("children", Items(Doc(("childId", "c-1"))))));
        Find("children.childId", "c-1").ShouldEqual(["b"]);
    }

    [Fact] void should_not_find_an_evicted_root()
    {
        Put("a", Doc(("joinKey", "j")));
        Find("joinKey", "j").ShouldEqual(["a"]);
        Evict("a");
        Find("joinKey", "j").ShouldBeEmpty();
    }

    [Fact] void should_not_find_anything_after_clear()
    {
        Put("a", Doc(("joinKey", "j")));
        Find("joinKey", "j").ShouldEqual(["a"]);
        cache.Clear();
        index.Clear();
        Find("joinKey", "j").ShouldBeEmpty();
    }

    [Fact] void should_not_find_a_root_removed_from_the_cache_without_notification()
    {
        Put("a", Doc(("joinKey", "j")));
        Find("joinKey", "j").ShouldEqual(["a"]);
        cache.TryRemove("a", out _);
        index.MarkDirty("a");
        Find("joinKey", "j").ShouldBeEmpty();
    }
}
