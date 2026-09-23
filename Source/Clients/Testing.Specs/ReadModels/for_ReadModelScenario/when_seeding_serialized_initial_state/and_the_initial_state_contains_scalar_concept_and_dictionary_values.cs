// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_serialized_initial_state;

public class and_the_initial_state_contains_scalar_concept_and_dictionary_values : Specification
{
    ReadModelScenario<SeededValuesReadModel> _scenario;
    SeededValuesReadModel _result;
    EventSourceId _id;
    Guid _idGuid;

    void Establish()
    {
        _idGuid = Guid.NewGuid();
        _id = new EventSourceId(_idGuid);
        _scenario = new ReadModelScenario<SeededValuesReadModel>(
            new SeededValuesReadModel(
                _idGuid,
                "seeded",
                42,
                new ConceptListItem("code"),
                new Dictionary<string, string> { ["Name"] = "upper", ["name"] = "lower" },
                "original note"));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new SeededValuesNoted("new note"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_complete_projection() => _result.ShouldNotBeNull();
    [Fact] void should_apply_the_event() => _result.Note.ShouldEqual("new note");
    [Fact] void should_preserve_the_seeded_name() => _result.Name.ShouldEqual("seeded");
    [Fact] void should_preserve_the_seeded_count() => _result.Count.ShouldEqual(42);
    [Fact] void should_preserve_the_seeded_concept() => _result.Code.ShouldEqual(new ConceptListItem("code"));
    [Fact] void should_preserve_the_exact_dictionary_entries() => _result.Labels.ShouldContainOnly(new KeyValuePair<string, string>("Name", "upper"), new KeyValuePair<string, string>("name", "lower"));
    [Fact] void should_preserve_the_event_source_id() => _result.Id.ShouldEqual(_idGuid);
    [Fact] void should_preserve_the_seeded_values_in_the_sink() => _scenario.InstanceForEventSourceId(_id)!.Count.ShouldEqual(42);
}
