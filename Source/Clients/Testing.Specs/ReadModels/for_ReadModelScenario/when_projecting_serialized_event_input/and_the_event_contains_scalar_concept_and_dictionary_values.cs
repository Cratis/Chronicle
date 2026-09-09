// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_serialized_event_input;

public class and_the_event_contains_scalar_concept_and_dictionary_values : Specification
{
    ReadModelScenario<SerializedValuesReadModel> _scenario;
    SerializedValuesReadModel _result;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<SerializedValuesReadModel>(null, new Defaults());
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(
            new SerializedValuesRecorded("first", 1, new ConceptListItem("old"), new Dictionary<string, string> { ["First"] = "first" }, "old wire value"),
            new SerializedValuesRecorded("last", 42, new ConceptListItem("code"), new Dictionary<string, string> { ["Name"] = "upper", ["name"] = "lower" }, "wire value"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_complete_projection() => _result.ShouldNotBeNull();
    [Fact] void should_apply_events_in_seed_order() => _result.Name.ShouldEqual("last");
    [Fact] void should_preserve_the_scalar_count() => _result.Count.ShouldEqual(42);
    [Fact] void should_round_trip_the_concept() => _result.Code.ShouldEqual(new ConceptListItem("code"));
    [Fact] void should_preserve_both_dictionary_keys() => _result.Labels.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_exact_dictionary_entries() => _result.Labels.ShouldContainOnly(new KeyValuePair<string, string>("Name", "upper"), new KeyValuePair<string, string>("name", "lower"));
    [Fact] void should_preserve_the_upper_case_dictionary_key() => _result.Labels["Name"].ShouldEqual("upper");
    [Fact] void should_preserve_the_lower_case_dictionary_key() => _result.Labels["name"].ShouldEqual("lower");
    [Fact] void should_map_the_serializer_owned_property_name() => _result.SerializedName.ShouldEqual("wire value");
    [Fact] void should_preserve_the_event_source_id() => _result.Id.ShouldEqual(Guid.Parse(_id.Value));
}
