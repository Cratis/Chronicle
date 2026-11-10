// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// A client reducer that did not declare page delivery keeps a step per partition, exactly as before.
/// </summary>
public class and_a_reducer_does_not_take_pages : given.a_catch_up_observer_job
{
    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer };
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed with { SubscriberType = typeof(IReducerObserverSubscriber) });
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys((Key)"partition-1", (Key)"partition-2"));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_a_step_per_partition() => _job.PreparedSteps!.Count.ShouldEqual(2);
    [Fact] void should_prepare_steps_that_handle_events_for_a_partition() => _job.PreparedSteps!.All(step => step.Type == typeof(IHandleEventsForPartition)).ShouldBeTrue();
}
