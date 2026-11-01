// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_building_a_second_path : an_index_over_a_cache
{
    string[] _found;
    string[] _expected;
    string[] _missing;

    void Establish()
    {
        Put("both", Doc(("joinKey", "a"), ("children", Items(Doc(("childId", "b"))))));
        Put("only-second", Doc(("children", Items(Doc(("childId", "b"))))));
        Find("joinKey", "a");
        Find("joinKey", "a");
        _expected = Scan("children.childId", "b");
    }

    void Because()
    {
        _found = Find("children.childId", "b");
        _missing = Find("children.childId", "missing");
    }

    [Fact] void should_scan_roots_without_remaining_dirty_marks() => _found.ShouldEqual(_expected);
    [Fact] void should_include_roots_with_only_the_second_path() => _found.Length.ShouldEqual(2);
    [Fact] void should_not_find_missing_values() => _missing.ShouldBeEmpty();
}
