// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes.given;

public class a_sink_with_two_joined_rows : Specification
{
    protected const ulong SeededAt = 10UL;
    protected const ulong WrittenAt = 20UL;

    protected readonly Exception _failure = new("Evaluation failed");
    protected InMemorySink _sink;
    protected ExpandoObject _initial;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected ResolvedJoin _failing;

    async Task Establish()
    {
        var readModel = new ReadModelDefinition(
            "test",
            "test",
            "test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []);
        _sink = new InMemorySink(readModel, new TypeFormats());
        await Seed("a", "first");
        await Seed("b", "second");
        _initial = (await _sink.FindOrDefault(KeyFor("a")))!.Clone();
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(_initial);
        _failing = Failing();
    }

    protected static Key KeyFor(string row) => new(row, ArrayIndexers.NoIndexers);

    protected PropertiesChanged<ExpandoObject> Set(string property, object? value) =>
        new(_initial, [new PropertyDifference(new PropertyPath(property), null, value)]);

    protected Joined RootJoin(string joinKey, string onProperty, params Change[] changes) =>
        new(_initial, joinKey, new PropertyPath(onProperty), ArrayIndexers.NoIndexers, changes);

    protected Joined ChildJoin(params Change[] changes) =>
        new(_initial, "child", new PropertyPath("[children].childId"), ChildIndexers, changes);

    protected PropertiesChanged<ExpandoObject> RenameChild() =>
        new(_initial, [new PropertyDifference(new PropertyPath("[children].name"), "before", "after", ChildIndexers)]);

    protected ChildRemovedFromAll RemoveChildFromAll() =>
        new(new PropertyPath("children"), new PropertyPath("childId"), "child", ArrayIndexers.NoIndexers);

    protected IDictionary<string, object?> Row(string row) => _sink.Collection[row];

    protected IDictionary<string, object?>[] Children(string row) =>
        ((IEnumerable<object>)Row(row)["children"]!).Cast<IDictionary<string, object?>>().ToArray();

    protected async Task<bool> AdmitsAGuardedWriteAtTheSameSequence(string row)
    {
        var retry = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        var current = (await _sink.FindOrDefault(KeyFor(row)))!.Clone();
        retry.InitialState.Returns(current);
        retry.Changes.Returns([new PropertiesChanged<ExpandoObject>(current, [new PropertyDifference(new PropertyPath("retried"), null, true)])]);
        await _sink.ApplyChanges(KeyFor(row), retry, WrittenAt, SinkWriteMode.OnlyWhenAdvancingWatermark);
        return Row(row).ContainsKey("retried");
    }

    void Destroy() => _sink.Dispose();

    ResolvedJoin Failing()
    {
        var nestedChanges = Substitute.For<IEnumerable<Change>>();
        nestedChanges.GetEnumerator().Returns(_ => throw _failure);
        return new ResolvedJoin(_initial, "first", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, nestedChanges);
    }

    static ArrayIndexers ChildIndexers => new([new ArrayIndexer("[children]", "childId", "child")]);

    async Task Seed(string row, string joinId)
    {
        var child = new ExpandoObject();
        var childFields = (IDictionary<string, object?>)child;
        childFields["childId"] = "child";
        childFields["name"] = "before";
        var seed = new ExpandoObject();
        var fields = (IDictionary<string, object?>)seed;
        fields["joinId"] = joinId;
        fields["group"] = "shared";
        fields["count"] = 0L;
        fields["children"] = new List<object> { child };
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(seed);
        changeset.Changes.Returns([]);
        await _sink.ApplyChanges(KeyFor(row), changeset, SeededAt);
    }
}
