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

public class and_a_cleared_nested_object_is_recreated : Specification
{
    InMemorySink _sink;
    Key _key;
    ExpandoObject? _cleared;
    ExpandoObject? _result;

    void Establish()
    {
        var definition = new ReadModelDefinition(
            "probe",
            "Probe",
            "Probe",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _sink = new InMemorySink(definition, new TypeFormats());
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
        initial.info = "root-info";
        var clear = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        clear.InitialState.Returns((ExpandoObject)initial);
        clear.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(_key, clear, 1UL);
        _cleared = await _sink.FindOrDefault(_key);

        dynamic recreatedInfo = new ExpandoObject();
        recreatedInfo.name = "Again";
        dynamic recreatedOuter = new ExpandoObject();
        recreatedOuter.info = recreatedInfo;
        dynamic changed = new ExpandoObject();
        changed.outer = recreatedOuter;
        var recreate = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        recreate.InitialState.Returns(_cleared);
        recreate.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)changed,
            [new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
        await _sink.ApplyChanges(_key, recreate, 2UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_clear_only_the_nested_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_cleared!)["outer"]!)["info"].ShouldBeNull();
    [Fact] void should_preserve_the_root_sibling() => ((IDictionary<string, object?>)_result!)["info"].ShouldEqual("root-info");
    [Fact] void should_recreate_the_nested_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)((IDictionary<string, object?>)_result!)["outer"]!)["info"]!)["name"].ShouldEqual("Again");
}
