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

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

/// <summary>
/// An event that adds a child can also update properties of the parent the child is added to - a parent's every-event
/// mappings applied to a child-only event. Both changes are keyed by the child, and both have to reach the parent.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_a_child_addition_also_updates_its_parent<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    const string Children = "children";
    const string ChildId = "childId";
    const string Parent = "parent";
    const string AddedChild = "added-child";
    const string Initial = "initial";
    const string Updated = "updated";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "lastUpdated": { "type": "string" },
            "children": { "type": "array", "items": { "type": "object", "properties": { "childId": { "type": "string" } } } }
          }
        }
        """;

    THarness _harness;
    ISink _sink;
    IDictionary<string, object?> _parent;

    async Task Establish()
    {
        _harness = CreateHarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
        var state = new ExpandoObject();
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(state, [
                new PropertyDifference(new PropertyPath("lastUpdated"), null, Initial),
                new PropertyDifference(new PropertyPath(Children), null, new List<ExpandoObject>())
            ])
        ];
        await _sink.ApplyChanges(new Key(Parent, ArrayIndexers.NoIndexers), Changeset(new ExpandoObject(), changes), 1UL);
    }

    async Task Because()
    {
        var childIndexer = new ArrayIndexers([new ArrayIndexer(new PropertyPath(Children), new PropertyPath(ChildId), AddedChild)]);
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)[ChildId] = AddedChild;
        var initialState = (await _sink.FindOrDefault(new Key(Parent, ArrayIndexers.NoIndexers)))!;
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("lastUpdated"), Initial, Updated, childIndexer)]),
            new ChildAdded(child, new PropertyPath(Children), new PropertyPath(ChildId), AddedChild, childIndexer)
        ];
        await _sink.ApplyChanges(new Key(Parent, childIndexer), Changeset(initialState, changes), 2UL);
        _parent = (await _sink.FindOrDefault(new Key(Parent, ArrayIndexers.NoIndexers)))!;
    }

    void Destroy() => _harness.Dispose();

    [Fact] public void should_update_the_parent() => _parent["lastUpdated"].ShouldEqual(Updated);
    [Fact] public void should_add_the_child() => ((IEnumerable<object>)_parent[Children]!).Cast<IDictionary<string, object?>>().Select(_ => _[ChildId]).ShouldEqual<IEnumerable<object?>>([AddedChild]);

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The <typeparamref name="THarness"/> to run the contract through.</returns>
    protected virtual THarness CreateHarness() => new();

    static IChangeset<AppendedEvent, ExpandoObject> Changeset(ExpandoObject initialState, Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initialState);
        changeset.Changes.Returns(changes);
        return changeset;
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "parents",
            "parents",
            "Parents",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson(SchemaJson) },
            []);
}
