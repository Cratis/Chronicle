// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

/// <summary>
/// An event reached the lane between the decision and the release: the queue refuses, and the observer leaves the ordinary way.
/// </summary>
public class when_the_queue_cannot_keep_the_subscription : given.a_subscribed_client_observer
{
    async Task Establish()
    {
        _appendedEventsQueues.TryMarkDormant(Arg.Any<ObserverKey>()).Returns(false);
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await Deactivate();

    [Fact] async Task should_have_asked_to_be_released() => await _appendedEventsQueues.Received(1).TryMarkDormant(_observerKey);
    [Fact] async Task should_unsubscribe_as_it_always_did() => await _appendedEventsQueues.Received().Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>());
}
