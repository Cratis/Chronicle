// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_answers_in_time : given.a_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [], _context);
        _completion.SetResult(ObserverSubscriberResult.Ok(42));
        _result = await delivery;
    }

    [Fact] void should_return_the_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
