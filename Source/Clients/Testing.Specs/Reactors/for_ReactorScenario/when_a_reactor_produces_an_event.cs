// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_produces_an_event : Specification
{
    const string MemberId = "member-42";

    ReactorScenario<ReservationReactor> _scenario;
    readonly EventSourceId _reservationId = EventSourceId.New();

    void Establish() => _scenario = new ReactorScenario<ReservationReactor>();

    async Task Because() => await _scenario.Given.ForEventSource(_reservationId).Events(new ReservationMade(MemberId));

    [Fact] void should_unwrap_and_produce_the_member_activity_recorded_event() => _scenario.ShouldHaveProduced<MemberActivityRecorded>();
}
