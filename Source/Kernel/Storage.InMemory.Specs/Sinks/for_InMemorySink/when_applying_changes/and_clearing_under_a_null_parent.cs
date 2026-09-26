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

public class and_clearing_under_a_null_parent : Specification
{
    InMemorySink _sink;
    Key _key;
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
        dynamic initial = new ExpandoObject();
        initial.outer = null;
        var clear = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        clear.InitialState.Returns((ExpandoObject)initial);
        clear.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(_key, clear, 1UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_leave_the_null_parent_untouched() => ((IDictionary<string, object?>)_result!)["outer"].ShouldBeNull();
}
