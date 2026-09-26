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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_a_deeply_nested_object_is_cleared_then_recreated : Specification
{
    SqlSinkHarness _harness;
    ExpandoObject? _result;

    void Establish() => _harness = new SqlSinkHarness();

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": { "outer": { "type": "object", "properties": {
                "info": { "type": "object", "properties": { "name": { "type": "string" } } }
            } } } }
            """);
        var definition = new ReadModelDefinition(
            "test",
            "Test",
            "test_read_models",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
            []);
        var sink = _harness.CreateSink(definition);
        var key = new Key("key", ArrayIndexers.NoIndexers);
        var created = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        created.InitialState.Returns(new ExpandoObject());
        created.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
            new PropertyDifference(new PropertyPath("outer.info.name"), null, "Before")])]);
        await sink.ApplyChanges(key, created, 1UL);

        var stateBeforeClear = await sink.FindOrDefault(key);
        var cleared = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        cleared.InitialState.Returns(stateBeforeClear);
        cleared.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        await sink.ApplyChanges(key, cleared, 2UL);

        var stateBeforeRecreate = await sink.FindOrDefault(key);
        var recreated = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        recreated.InitialState.Returns(stateBeforeRecreate);
        recreated.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
            new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
        await sink.ApplyChanges(key, recreated, 3UL);
        _result = await sink.FindOrDefault(key);
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_recreate_the_nested_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)((IDictionary<string, object?>)_result!)["outer"]!)["info"]!)["name"].ShouldEqual("Again");
    [Fact] void should_not_clear_the_root() => ((IDictionary<string, object?>)_result!).ContainsKey("info").ShouldBeFalse();
}
