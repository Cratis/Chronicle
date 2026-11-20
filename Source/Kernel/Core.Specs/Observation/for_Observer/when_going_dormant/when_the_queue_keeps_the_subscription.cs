// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_the_queue_keeps_the_subscription : given.a_subscribed_client_observer
{
    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await Deactivate();

    [Fact] async Task should_have_asked_the_queue_to_keep_it() => await _appendedEventsQueues.Received(1).TryMarkDormant(_observerKey);
    [Fact] async Task should_not_unsubscribe() => await _appendedEventsQueues.DidNotReceive().Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>());
    [Fact] void should_keep_the_record_of_the_subscription() => Recorded.ShouldNotBeNull();
}
