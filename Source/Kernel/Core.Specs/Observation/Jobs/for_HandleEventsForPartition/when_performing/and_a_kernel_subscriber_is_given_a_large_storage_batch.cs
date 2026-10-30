// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// Kernel-owned subscribers keep the storage batches they are tuned for.
/// </summary>
public class and_a_kernel_subscriber_is_given_a_large_storage_batch : given.a_performing_job_step
{
    readonly List<int> _callSizes = [];

    void Establish()
    {
        _observersConfig = new Observers { MaxEventsPerSubscriberCall = 100 };
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _callSizes.Add(events.Length);
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
        _eventCursor.Current.Returns(
            Enumerable.Range(0, 250).Select(offset => AppendedEvent.EmptyWithEventSequenceNumber(first_event_sequence_number + (ulong)offset)).ToArray());
    }

    Task Because() => _jobStep.InvokePerformStep(_performState);

    [Fact] void should_hand_the_storage_batch_over_in_one_call() => _callSizes.ShouldContainOnly(250);
}
