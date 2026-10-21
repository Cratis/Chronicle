// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_deadline_passed_before_dispatch : given.a_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(31));
        _result = await _subscriber.OnNext("partition", [], _context);
    }

    [Fact] void should_not_call_the_mediator() => _mediator.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_fail_the_ghost_delivery() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
