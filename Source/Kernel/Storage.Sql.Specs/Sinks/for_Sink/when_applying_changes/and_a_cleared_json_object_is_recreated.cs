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
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_a_cleared_json_object_is_recreated : Specification
{
    readonly SqlSinkHarness _harness = new();
    ISink _sink;
    Key _key;
    ExpandoObject? _cleared;
    ExpandoObject? _result;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            { "type": "object", "properties": {
              "id": { "type": "string" },
              "outer": { "type": "object", "properties": {
                "info": { "type": "object", "properties": { "name": { "type": "string" } } }
              } }
            } }
            """);
        var definition = new ReadModelDefinition(
            "probe",
            "Probe",
            "Probe",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
            []);
        _sink = _harness.CreateSink(definition);
        _key = new Key("probe-1", ArrayIndexers.NoIndexers);
    }

    async Task Because()
    {
        dynamic info = new ExpandoObject();
        info.name = "First";
        dynamic outer = new ExpandoObject();
        outer.info = info;
        dynamic initial = new ExpandoObject();
        initial.outer = outer;
        var seed = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        seed.InitialState.Returns(new ExpandoObject());
        seed.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)initial,
            [new PropertyDifference(new PropertyPath("outer"), null, (ExpandoObject)outer)])]);
        await _sink.ApplyChanges(_key, seed, 1UL);

        var clear = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        var beforeClear = await _sink.FindOrDefault(_key);
        clear.InitialState.Returns(beforeClear);
        clear.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(_key, clear, 2UL);
        _cleared = await _sink.FindOrDefault(_key);

        dynamic changed = new ExpandoObject();
        changed.outer = new ExpandoObject();
        changed.outer.info = new ExpandoObject();
        changed.outer.info.name = "Again";
        var recreate = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        recreate.InitialState.Returns(_cleared);
        recreate.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)changed,
            [new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
        await _sink.ApplyChanges(_key, recreate, 3UL);
        _result = await _sink.FindOrDefault(_key);
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_clear_the_nested_json_object() => (((IDictionary<string, object?>)((IDictionary<string, object?>)_cleared!)["outer"]!).TryGetValue("info", out var value) && value is not null).ShouldBeFalse();
    [Fact] void should_recreate_the_nested_json_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)((IDictionary<string, object?>)_result!)["outer"]!)["info"]!)["name"].ShouldEqual("Again");
}
