// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_dirty_parents_have_nested_collections : given.a_bulk_child_removal_from_all
{
    readonly Key _key = new("parent", ArrayIndexers.NoIndexers);
    readonly List<ExpandoObject> _firstChildren = [];
    readonly List<ExpandoObject> _secondChildren = [];
    ExpandoObject _parent;
    ExpandoObject _remaining;
    ExpandoObject? _result;

    async Task Establish()
    {
        _remaining = Document("id", "keep");
        _firstChildren.AddRange([Document("id", "item"), _remaining]);
        _secondChildren.Add(Document("id", "item"));
        _parent = Document("groups", new List<object?>
        {
            Document("details", Document("items", _firstChildren)),
            Document("details", Document("items", _secondChildren)),
            Document("details", Document("items", null)),
            Document("details", Document("items", "not a collection")),
            Document("details", new ExpandoObject()),
            Document("details", null),
            new ExpandoObject(),
            null,
            "not a document"
        });
        await Cache(_key, _parent);
        _changeset.Changes.Returns([new ChildRemovedFromAll("[groups].details.[items]", "id", "item", ArrayIndexers.NoIndexers)]);
        _changeset.CurrentState.Returns(new ExpandoObject());
    }

    async Task Because()
    {
        await _sink.ApplyChanges(new Key("child", ArrayIndexers.NoIndexers), _changeset, (EventSequenceNumber)2UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_preserve_the_dirty_parent() => ReferenceEquals(_result, _parent).ShouldBeTrue();
    [Fact] void should_remove_only_the_matching_child_from_the_first_nested_collection() => _firstChildren.ShouldEqual<IEnumerable<ExpandoObject>>([_remaining]);
    [Fact] void should_remove_the_matching_child_from_the_second_nested_collection() => _secondChildren.ShouldBeEmpty();
}
