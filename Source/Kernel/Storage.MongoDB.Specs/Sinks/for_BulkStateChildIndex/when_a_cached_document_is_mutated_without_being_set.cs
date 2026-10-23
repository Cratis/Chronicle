// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_cached_document_is_mutated_without_being_set : given.an_index
{
    readonly ExpandoObject _child = Document(("id", 42));
    readonly List<object> _children = [];
    string[] _newMatches;
    string[] _laterMatches;
    string[] _addedMatches;
    string[] _removedMatches;

    void Establish()
    {
        _children.Add(_child);
        Set("root", Document(("children", _children)));
        Find("[children].id", 42);
        _index.MarkExposed("root");
    }

    void Because()
    {
        ((IDictionary<string, object?>)_child)["id"] = 99;
        _matches = Find("[children].id", 42);
        _newMatches = Find("[children].id", 99);
        ((IDictionary<string, object?>)_child)["id"] = 100;
        _laterMatches = Find("[children].id", 100);
        _children.Add(Document(("id", 101)));
        _addedMatches = Find("[children].id", 101);
        _children.Clear();
        _removedMatches = Find("[children].id", 100);
    }

    [Fact] void should_remove_the_old_value() => _matches.ShouldBeEmpty();
    [Fact] void should_find_the_new_value_without_a_set() => _newMatches.ShouldContainOnly("root");
    [Fact] void should_refresh_again_when_the_retained_reference_is_mutated() => _laterMatches.ShouldContainOnly("root");
    [Fact] void should_find_a_child_added_to_an_exposed_collection() => _addedMatches.ShouldContainOnly("root");
    [Fact] void should_forget_a_child_removed_from_an_exposed_collection() => _removedMatches.ShouldBeEmpty();
}
