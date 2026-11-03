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
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout.given;

public abstract class a_sink_over_a_schema_with_an_identifier : Specification
{
    protected const string Table = "keyed_models";
    protected SqlSinkHarness _harness;
    protected Sink _sink;
    protected int _layoutReads;

    protected abstract string Identifier { get; }

    void Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = JsonSchema.FromJson($$$"""{"type":"object","properties":{"{{{Identifier}}}":{"type":"string"},"name":{"type":"string"} }}""");
        _sink = (Sink)_harness.CreateSink(new ReadModelDefinition("keyed", Table, "Keyed", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "keyed", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        _sink.ReadTableLayout = async (database, type, table) =>
        {
            _layoutReads++;
            return await PrimaryKeyColumns.LayoutOf(database, type, table);
        };
    }

    protected Task Write(string key, string name)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        var initialState = new ExpandoObject();
        ((IDictionary<string, object?>)initialState)["Id"] = key;
        changeset.InitialState.Returns(initialState);
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, name)])]);
        return _sink.ApplyChanges(new Key(key, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
    }

    protected async Task Replay()
    {
        var context = new ReplayContext(new ReadModelType("keyed", ReadModelGeneration.First), Table, "keyed_models_backup", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(context);
        await Write("one", "replayed first");
        await Write("two", "replayed second");
        await _sink.EndReplay(context);
        await _sink.FindOrDefault(new Key("one", ArrayIndexers.NoIndexers));
    }

    void Destroy() => _harness.Dispose();
}
