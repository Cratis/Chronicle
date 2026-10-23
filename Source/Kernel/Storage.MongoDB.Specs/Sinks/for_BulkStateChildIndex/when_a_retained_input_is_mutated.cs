// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_retained_input_is_mutated : given.an_index
{
    readonly ExpandoObject _child = Document(("id", 42));
    readonly List<object> _children = [];
    string[] _changedMatches;
    string[] _addedMatches;
    string[] _scannedMatches;

    void Establish()
    {
        _children.Add(_child);
        Set("root", Document(("children", _children)));
        Find("[children].id", 42);
    }

    void Because()
    {
        ((IDictionary<string, object?>)_child)["id"] = 99;
        _children.Add(Document(("id", 100)));
        _matches = Find("[children].id", 42);
        _changedMatches = Find("[children].id", 99);
        _addedMatches = Find("[children].id", 100);
        var path = new PropertyPath("[children].id");
        _scannedMatches = _states.Where(pair => Sink.TryFindValueInDocument(pair.Value, path.Segments.ToArray(), 0, 42)).Select(pair => pair.Key).ToArray();
    }

    [Fact] void should_keep_the_set_value() => _matches.ShouldContainOnly("root");
    [Fact] void should_not_index_a_retained_reference_mutation() => _changedMatches.ShouldBeEmpty();
    [Fact] void should_not_index_a_retained_collection_addition() => _addedMatches.ShouldBeEmpty();
    [Fact] void should_match_the_scan_of_cache_owned_state() => _matches.SequenceEqual(_scannedMatches).ShouldBeTrue();
}
