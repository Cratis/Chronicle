// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_cached_document_is_mutated_in_place : an_index_over_a_cache
{
    [Fact] void should_find_a_child_added_in_place_after_the_document_was_marked_dirty()
    {
        var document = Doc(("children", Items()));
        Put("a", document);
        Find("children.childId", "c-1").ShouldBeEmpty();
        index.MarkDirty("a");
        ((List<object>)((IDictionary<string, object?>)document)["children"]!).Add(Doc(("childId", "c-1")));
        Find("children.childId", "c-1").ShouldEqual(["a"]);
    }

    [Fact] void should_not_find_a_child_removed_in_place_even_without_a_dirty_mark()
    {
        var child = Doc(("childId", "c-1"));
        var document = Doc(("children", Items(child)));
        Put("a", document);
        Find("children.childId", "c-1").ShouldEqual(["a"]);
        ((List<object>)((IDictionary<string, object?>)document)["children"]!).Clear();
        Find("children.childId", "c-1").ShouldBeEmpty();
    }
}
