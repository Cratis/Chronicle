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

public class and_a_nested_object_is_cleared_with_no_outer_object : Specification
{
    InMemorySink _sink;
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
        dynamic initial = new ExpandoObject();
        initial.outer = null;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns((ExpandoObject)initial);
        changeset.Changes.Returns([new NestedCleared("outer.info", ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(key, changeset, 1UL);
        _result = (await _sink.FindOrDefault(key))!;
    }

    [Fact] void should_leave_the_outer_object_null() => ((IDictionary<string, object?>)_result)["outer"].ShouldBeNull();
}
