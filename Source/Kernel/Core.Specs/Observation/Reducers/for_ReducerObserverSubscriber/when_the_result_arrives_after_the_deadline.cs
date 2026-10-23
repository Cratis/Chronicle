// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber;

public class when_the_result_arrives_after_the_deadline : given.a_dispatching_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [], _context);
        _clock.Advance(TimeSpan.FromSeconds(31));
        _completion.SetResult(new(ObserverSubscriberResult.Ok(42), new()));
        _result = await delivery;
    }

    [Fact] void should_discard_the_abandoned_result() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_reach_the_sink_write() => _sinkReached.ShouldBeFalse();
}
