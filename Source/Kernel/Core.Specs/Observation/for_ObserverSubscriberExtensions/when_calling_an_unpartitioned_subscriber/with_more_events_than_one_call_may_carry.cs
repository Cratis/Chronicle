// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_an_unpartitioned_subscriber;

public class with_more_events_than_one_call_may_carry : given.an_unpartitioned_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNextWithin(TimeSpan.FromMinutes(1), "some-partition", _events, new ObserverSubscriberContext(null));

    [Fact] void should_hand_the_events_over_in_bounded_calls() => _batchSizes.ShouldEqual([20, 20, 10]);
    [Fact] void should_be_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_last_event_of_the_last_call() => _result.LastSuccessfulObservation.Value.ShouldEqual(49UL);
}
