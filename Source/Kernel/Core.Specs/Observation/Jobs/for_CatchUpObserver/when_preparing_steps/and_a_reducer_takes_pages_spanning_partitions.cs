// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// A client reducer that declared page delivery is caught up in one walk, so the pages it receives span partitions.
/// </summary>
public class and_a_reducer_takes_pages_spanning_partitions : given.a_catch_up_observer_job
{
    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer };
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed with { SubscriberType = typeof(IReducerPageObserverSubscriber) });
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys((Key)"partition-1", (Key)"partition-2"));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_a_single_step() => _job.PreparedSteps!.Count.ShouldEqual(1);
    [Fact] void should_prepare_a_step_that_handles_events_for_the_whole_observer() => _job.PreparedSteps!.Single().Type.ShouldEqual(typeof(IHandleEventsForObserver));
}
