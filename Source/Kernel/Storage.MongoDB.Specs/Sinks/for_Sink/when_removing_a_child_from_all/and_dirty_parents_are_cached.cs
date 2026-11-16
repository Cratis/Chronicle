// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_dirty_parents_are_cached : given.a_bulk_child_removal_from_all
{
    readonly Key _firstKey = new("first", ArrayIndexers.NoIndexers);
    readonly Key _secondKey = new("second", ArrayIndexers.NoIndexers);
    readonly List<ExpandoObject> _firstChildren = [];
    readonly List<ExpandoObject> _secondChildren = [];
    ExpandoObject _firstParent;
    ExpandoObject _secondParent;
    ExpandoObject _remaining;
    ExpandoObject? _firstResult;
    ExpandoObject? _secondResult;

    async Task Establish()
    {
        _remaining = Document("childId", "keep");
        _firstChildren.AddRange([Document("childId", "item"), _remaining]);
        _secondChildren.Add(Document("childId", "item"));
        _firstParent = Document("items", _firstChildren);
        _secondParent = Document("items", _secondChildren);
        await Cache(_firstKey, _firstParent);
        await Cache(_secondKey, _secondParent);
        _changeset.Changes.Returns([new ChildRemovedFromAll("[items]", "childId", "item", ArrayIndexers.NoIndexers)]);
        _changeset.CurrentState.Returns(new ExpandoObject());
    }

    async Task Because()
    {
        await _sink.ApplyChanges(new Key("child", ArrayIndexers.NoIndexers), _changeset, (EventSequenceNumber)2UL);
        _firstResult = await _sink.FindOrDefault(_firstKey);
        _secondResult = await _sink.FindOrDefault(_secondKey);
    }

    [Fact] void should_preserve_the_first_dirty_parent() => ReferenceEquals(_firstResult, _firstParent).ShouldBeTrue();
    [Fact] void should_preserve_the_second_dirty_parent() => ReferenceEquals(_secondResult, _secondParent).ShouldBeTrue();
    [Fact] void should_remove_only_the_matching_child_from_the_first_parent() => _firstChildren.ShouldEqual<IEnumerable<ExpandoObject>>([_remaining]);
    [Fact] void should_remove_the_matching_child_from_the_second_parent() => _secondChildren.ShouldBeEmpty();
}
