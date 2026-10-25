// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

/// <summary>
/// A collapsing projection writes to documents shared by several partitions that the read model watermark cannot
/// guard, so a crash must lose at most the slice in flight. Every slice is therefore checkpointed, whatever the
/// configured interval, where a partitioned subscriber keeps the debounce.
/// </summary>
public class and_a_collapsing_subscriber_is_handled_with_a_checkpoint_interval : given.a_debouncing_job_step
{
    void Establish()
    {
        var subscription = new ObserverSubscription(
            "observer-id",
            _performState.ObserverKey,
            [],
            typeof(Projections.ICollapsingProjectionObserverSubscriber),
            SiloAddress.Zero);
        _observer.GetSubscription().Returns(subscription);
        var collapsing = Substitute.For<Projections.ICollapsingProjectionObserverSubscriber>();
        collapsing.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Task.FromResult(ObserverSubscriberResult.Ok(call.ArgAt<IEnumerable<AppendedEvent>>(1).Last().Context.SequenceNumber)));
        _silo.AddProbe<Projections.ICollapsingProjectionObserverSubscriber>(_ => collapsing);
        _jobStep.SetupForTesting(_observer, subscription);

        _performState.EventObservationState = EventObservationState.None;
        _jobStep.MaxEventsPerBatch = 1;
        _eventCursor.Current.Returns(Enumerable.Range(1, 4).Select(_ => CreateEvent((ulong)_, "module")).ToArray());
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_checkpoint_every_slice_although_the_interval_is_larger() => (_stateStorageStats.Writes >= 4).ShouldBeTrue();
}
