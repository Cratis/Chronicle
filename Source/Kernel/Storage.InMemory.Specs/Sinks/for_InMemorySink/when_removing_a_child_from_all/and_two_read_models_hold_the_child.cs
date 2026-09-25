// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_removing_a_child_from_all;

public class and_two_read_models_hold_the_child : Specification
{
    const string ChildrenProperty = "children";
    const string IdentifiedByProperty = "childId";

    InMemorySink _sink;
    Key _written;
    Key _other;
    IEnumerable<string> _remainingOnWritten;
    IEnumerable<string> _remainingOnOther;

    async Task Establish()
    {
        _written = new Key("parent-1", ArrayIndexers.NoIndexers);
        _other = new Key("parent-2", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        await _sink.ApplyChanges(_written, ChangesetAddingChildren("shared-child", "kept-on-written"), 1UL);
        await _sink.ApplyChanges(_other, ChangesetAddingChildren("shared-child", "kept-on-other"), 1UL);
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_written, ChangesetRemovingChildFromAll("shared-child", (await _sink.FindOrDefault(_written))!), 2UL);
        _remainingOnWritten = await ChildIdsFor(_written);
        _remainingOnOther = await ChildIdsFor(_other);
    }

    [Fact] void should_remove_the_child_from_the_read_model_being_written() => _remainingOnWritten.ShouldContainOnly(["kept-on-written"]);
    [Fact] void should_remove_the_child_from_the_other_read_model() => _remainingOnOther.ShouldContainOnly(["kept-on-other"]);

    static IChangeset<AppendedEvent, ExpandoObject> ChangesetAddingChildren(params string[] childIds)
    {
        var changes = childIds.Select(childId =>
        {
            var child = new ExpandoObject();
            ((IDictionary<string, object?>)child)[IdentifiedByProperty] = childId;

            return (Change)new ChildAdded(
                child,
                new PropertyPath(ChildrenProperty),
                new PropertyPath(IdentifiedByProperty),
                childId,
                ArrayIndexers.NoIndexers);
        }).ToArray();

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);
        return changeset;
    }

    static IChangeset<AppendedEvent, ExpandoObject> ChangesetRemovingChildFromAll(string childId, ExpandoObject initialState) =>
        ChangesetWith(initialState, new ChildRemovedFromAll(
            new PropertyPath(ChildrenProperty),
            new PropertyPath(IdentifiedByProperty),
            childId,
            ArrayIndexers.NoIndexers));

    static IChangeset<AppendedEvent, ExpandoObject> ChangesetWith(ExpandoObject initialState, Change change)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initialState);
        Change[] changes = [change];
        changeset.Changes.Returns(changes);
        return changeset;
    }

    async Task<IEnumerable<string>> ChildIdsFor(Key key)
    {
        var instance = await _sink.FindOrDefault(key);
        if (!((IDictionary<string, object?>)instance!).TryGetValue(ChildrenProperty, out var children) || children is null)
        {
            return [];
        }

        return ((IEnumerable<object>)children)
            .Select(child => (string)((IDictionary<string, object?>)child)[IdentifiedByProperty]!)
            .ToArray();
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
