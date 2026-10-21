// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_disconnects_while_abandoned : given.a_subscriber
{
    ObserverSubscriberResult _result;
    bool _held;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [], _context);
        _clock.Advance(TimeSpan.FromSeconds(31));
        _held = !delivery.IsCompleted;
        _completion.SetCanceled();
        _result = await delivery;
    }

    [Fact] void should_hold_the_turn_until_the_disconnect() => _held.ShouldBeTrue();
    [Fact] void should_release_the_turn_on_disconnect() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
