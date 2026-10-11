// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_mixed_events : Specification
{
    ReactorScenario<MixedReservationReactor> _scenario;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given.ForEventSource(_source).Events(new ReservationMade("member"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_record_both_events() => _scenario.ProducedEvents.Count.ShouldEqual(2);
    [Fact] void should_route_the_bare_event_to_the_triggering_source() => _scenario.ProducedEvents[0].EventSourceId.ShouldEqual(_source);
    [Fact] void should_route_the_wrapped_event_to_its_own_source() => _scenario.ProducedEvents[^1].EventSourceId.ShouldEqual(new EventSourceId("member"));
    [Fact] void should_preserve_the_wrappers_default_route() => _scenario.ProducedEvents[^1].ShouldEqual(new ProducedEvent(new MemberActivityRecorded(), "member", EventSourceType.Default, EventStreamType.All, EventStreamId.Default));
}
