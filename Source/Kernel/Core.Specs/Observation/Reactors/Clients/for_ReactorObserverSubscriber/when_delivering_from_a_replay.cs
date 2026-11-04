// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_delivering_from_a_replay : given.a_subscriber
{
    void Establish() => _context = _context with { Origin = ObservationOrigin.Replay };

    void Because() => _ = _subscriber.OnNext("partition", [], _context);

    [Fact] void should_tell_the_client_the_events_come_from_a_replay() => _origin.ShouldEqual(ObservationOrigin.Replay);
}
