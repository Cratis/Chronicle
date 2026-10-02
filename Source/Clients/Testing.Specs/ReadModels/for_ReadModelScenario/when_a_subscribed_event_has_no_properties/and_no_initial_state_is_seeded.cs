// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_subscribed_event_has_no_properties;

public class and_no_initial_state_is_seeded : Specification
{
    ReadModelScenario<MarkerState> _scenario;
    Guid _id;

    void Establish()
    {
        _id = Guid.NewGuid();
        _scenario = new ReadModelScenario<MarkerState>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(new EventSourceId(_id))
            .Events(new Marked());

    [Fact] void should_create_the_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_identify_the_instance_by_the_event_source() => _scenario.Instance!.Id.ShouldEqual(_id);
    [Fact] void should_leave_unmapped_properties_at_their_defaults() => _scenario.Instance!.IsMarked.ShouldBeFalse();
}
