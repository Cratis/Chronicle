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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes.given;

public class a_sink_with_same_row_direct_and_join_changes : Specification
{
    protected InMemorySink _sink;
    protected Key _key;
    protected ExpandoObject _initial;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;

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
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromType<TestReadModel>()
            },
            []);
        _sink = new InMemorySink(readModel, new TypeFormats());
        _key = new Key("root", ArrayIndexers.NoIndexers);
        var seed = new ExpandoObject();
        var fields = (IDictionary<string, object?>)seed;
        fields["id"] = "root";
        fields["joinId"] = "root";
        fields["count"] = 0L;
        fields["retained"] = "untouched";
        var seedChangeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        seedChangeset.InitialState.Returns(seed);
        seedChangeset.Changes.Returns([]);
        await _sink.ApplyChanges(_key, seedChangeset, 10UL);
        _initial = (await _sink.FindOrDefault(_key))!.Clone();
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(_initial);
    }

    protected PropertiesChanged<ExpandoObject> Direct(long count) =>
        new(_initial, [new PropertyDifference(new PropertyPath("count"), 0L, count)]);

    protected Joined Join() => new(
        _initial, "root", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(2L)])
    {
        HasKeyedFrom = true
    };

    protected long StoredCount => (long)((IDictionary<string, object?>)_sink.Collection["root"])["count"]!;

    void Destroy() => _sink.Dispose();

    record TestReadModel(string Id, string JoinId, long Count, string Retained);
}
