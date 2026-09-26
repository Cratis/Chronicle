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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_deeply_nested_object_is_cleared_then_recreated : Specification
{
    InMemorySink _sink;
    ExpandoObject _clearedState;
    ExpandoObject _result;

    void Establish()
    {
        var definition = new ReadModelDefinition(
            "test",
            "Test",
            "Test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _sink = new InMemorySink(definition, new TypeFormats());
    }

    async Task Because()
    {
        var key = new Key("key", ArrayIndexers.NoIndexers);
        dynamic original = new ExpandoObject();
        original.outer = new ExpandoObject();
        original.outer.info = new ExpandoObject();
        original.outer.info.name = "Before";
        var created = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        created.InitialState.Returns((ExpandoObject)original);
        created.Changes.Returns([]);
        await _sink.ApplyChanges(key, created, 1UL);

        var cleared = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        cleared.InitialState.Returns((await _sink.FindOrDefault(key))!);
        cleared.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(key, cleared, 2UL);
        _clearedState = (await _sink.FindOrDefault(key))!;

        var recreated = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        recreated.InitialState.Returns((await _sink.FindOrDefault(key))!);
        recreated.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
            new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
        await _sink.ApplyChanges(key, recreated, 3UL);
        _result = (await _sink.FindOrDefault(key))!;
    }

    [Fact] void should_clear_the_inner_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_clearedState)["outer"]!)["info"].ShouldBeNull();
    [Fact] void should_recreate_the_nested_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)((IDictionary<string, object?>)_result)["outer"]!)["info"]!)["name"].ShouldEqual("Again");
    [Fact] void should_not_clear_a_root_property() => ((IDictionary<string, object?>)_result).ContainsKey("info").ShouldBeFalse();
}
