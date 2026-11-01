// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_child_key_changes : an_index_over_a_cache
{
    [Fact] void should_follow_the_new_key_when_the_document_is_put_again()
    {
        Put("a", Doc(("children", Items(Doc(("childId", "A"))))));
        Find("children.childId", "A").ShouldEqual(["a"]);
        Put("a", Doc(("children", Items(Doc(("childId", "B"))))));
        Find("children.childId", "A").ShouldBeEmpty();
        Find("children.childId", "B").ShouldEqual(["a"]);
        Find("children.childId", "A").ShouldEqual(Scan("children.childId", "A"));
        Find("children.childId", "B").ShouldEqual(Scan("children.childId", "B"));
    }

    [Fact] void should_follow_the_new_key_when_it_is_renamed_in_place_and_marked_dirty()
    {
        var child = Doc(("childId", "A"));
        Put("a", Doc(("children", Items(child))));
        Find("children.childId", "A").ShouldEqual(["a"]);
        ((IDictionary<string, object?>)child)["childId"] = "B";
        index.MarkDirty("a");
        Find("children.childId", "A").ShouldBeEmpty();
        Find("children.childId", "B").ShouldEqual(["a"]);
        Find("children.childId", "A").ShouldEqual(Scan("children.childId", "A"));
        Find("children.childId", "B").ShouldEqual(Scan("children.childId", "B"));
    }
}
