// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_projecting_a_concept_collection : Specification
{
    ReadModelScenario<ConceptListReadModel> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<ConceptListReadModel>();
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new ConceptListSet([new ConceptListItem("a"), new ConceptListItem("b")]));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_items() => _scenario.Instance!.Items.Count.ShouldEqual(2);
    [Fact] void should_round_trip_first_item() => _scenario.Instance!.Items[0].Value.ShouldEqual("a");
    [Fact] void should_round_trip_second_item() => _scenario.Instance!.Items[1].Value.ShouldEqual("b");
}
