// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_answers_after_the_deadline : given.a_subscriber
{
    ObserverSubscriberResult _result;
    bool _held;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [], _context);
        _clock.Advance(TimeSpan.FromSeconds(31));
        _held = !delivery.IsCompleted;
        _completion.SetResult(ObserverSubscriberResult.Ok(42));
        _result = await delivery;
    }

    [Fact] void should_hold_the_turn_until_the_late_result() => _held.ShouldBeTrue();
    [Fact] void should_not_report_late_success_as_recovery() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
