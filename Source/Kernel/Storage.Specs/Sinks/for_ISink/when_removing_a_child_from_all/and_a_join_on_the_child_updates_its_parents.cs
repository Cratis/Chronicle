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

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child_from_all;

/// <summary>
/// An event that removes a child from every parent can also carry a join on that child, which updates the parents
/// holding it - a parent's every-event mappings applied to a child-only removal. The join is matched by the child, so
/// it has to reach the parents as they were before the removal; applied afterwards it matches nothing.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_a_join_on_the_child_updates_its_parents<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    const string Children = "children";
    const string ChildId = "childId";
    const string RemovedChild = "removed-child";
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
    IDictionary<string, object?> _first;
    IDictionary<string, object?> _second;
    IDictionary<string, object?> _unrelated;

    async Task Establish()
    {
        _harness = CreateHarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
        await Seed("first", RemovedChild, "kept-on-first");
        await Seed("second", RemovedChild);
        await Seed("unrelated", "other-child");
    }

    async Task Because()
    {
        var childIndexer = new ArrayIndexers([new ArrayIndexer(new PropertyPath(Children), new PropertyPath(ChildId), RemovedChild)]);
        var parentUpdate = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("lastUpdated"), Initial, Updated)]);
        Change[] changes =
        [
            new Joined(new ExpandoObject(), RemovedChild, new PropertyPath(Children) + new PropertyPath(ChildId), childIndexer, [parentUpdate]),
            new ChildRemovedFromAll(new PropertyPath(Children), new PropertyPath(ChildId), RemovedChild, ArrayIndexers.NoIndexers)
        ];
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(new Key(RemovedChild, childIndexer), changeset, 2UL);

        _first = (await Find("first"))!;
        _second = (await Find("second"))!;
        _unrelated = (await Find("unrelated"))!;
    }

    void Destroy() => _harness.Dispose();

    [Fact] public void should_update_every_parent_that_held_the_child() => new[] { _first["lastUpdated"], _second["lastUpdated"] }.ShouldEqual<IEnumerable<object?>>([Updated, Updated]);
    [Fact] public void should_not_update_a_parent_without_the_child() => _unrelated["lastUpdated"].ShouldEqual(Initial);
    [Fact] public void should_remove_the_child_from_every_parent() => ChildIds(_first).Concat(ChildIds(_second)).ShouldEqual<IEnumerable<string>>(["kept-on-first"]);
    [Fact] public void should_leave_the_children_of_a_parent_without_the_child() => ChildIds(_unrelated).ShouldEqual<IEnumerable<string>>(["other-child"]);

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The <typeparamref name="THarness"/> to run the contract through.</returns>
    protected virtual THarness CreateHarness() => new();

    static IEnumerable<string> ChildIds(IDictionary<string, object?> parent) =>
        ((IEnumerable<object>)parent[Children]!).Cast<IDictionary<string, object?>>().Select(child => (string)child[ChildId]!).ToArray();

    static ExpandoObject Child(string id)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)[ChildId] = id;
        return child;
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

    async Task Seed(string parent, params string[] childIds)
    {
        var state = new ExpandoObject();
        var children = childIds.Select(Child).ToList();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(state, [
                new PropertyDifference(new PropertyPath("lastUpdated"), null, Initial),
                new PropertyDifference(new PropertyPath(Children), null, children)
            ])
        ];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(new Key(parent, ArrayIndexers.NoIndexers), changeset, 1UL);
    }

    async Task<IDictionary<string, object?>?> Find(string parent) => await _sink.FindOrDefault(new Key(parent, ArrayIndexers.NoIndexers));
}
