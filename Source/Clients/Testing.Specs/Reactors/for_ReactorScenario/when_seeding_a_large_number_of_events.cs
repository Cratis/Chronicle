// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_seeding_a_large_number_of_events : Specification
{
    const int NumberOfReservations = 1000;

    ReactorScenario<ReservationReactor> _scenario;
    EventSourceId _reservationId;

    void Establish()
    {
        _scenario = new ReactorScenario<ReservationReactor>();
        _reservationId = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_reservationId)
        .Events(Enumerable.Range(0, NumberOfReservations).Select(_ => new ReservationMade($"member-{_}")).ToArray<object>());

    [Fact] void should_produce_a_side_effect_per_event() => _scenario.Produced.OfType<MemberActivityRecorded>().Count().ShouldEqual(NumberOfReservations);
    [Fact] void should_not_produce_anything_else() => _scenario.Produced.Count.ShouldEqual(NumberOfReservations);
}
