// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_produces_concurrency_scoped_events : Specification
{
    ReactorScenario<ConcurrencyScopedReservationReactor> _scenario;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade("member-42"));

    [Fact] void should_unwrap_and_produce_the_event() => _scenario.ShouldHaveProduced<MemberActivityRecorded>();
}
