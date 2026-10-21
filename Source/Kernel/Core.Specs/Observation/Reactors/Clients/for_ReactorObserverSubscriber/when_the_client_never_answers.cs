// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_never_answers : given.a_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [], _context);
        _clock.Advance(TimeSpan.FromSeconds(331));
        _result = await delivery;
    }

    [Fact] void should_release_at_the_maximum_hold() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_claim_the_client_work_ended() => _completion.Task.IsCompleted.ShouldBeFalse();
}
