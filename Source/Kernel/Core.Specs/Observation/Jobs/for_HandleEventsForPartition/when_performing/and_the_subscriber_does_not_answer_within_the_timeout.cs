// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// Proves the step actually consults the observer's configured subscriber timeout - the knob existed for a long time
/// without anything ever reading it. The bounding itself is specified against
/// <see cref="ObserverSubscriberExtensions.OnNextWithin"/>; this is about the wiring.
/// </summary>
public class and_the_subscriber_does_not_answer_within_the_timeout : given.a_performing_job_step
{
    /// <summary>
    /// Slow on purpose - infinitely so - because the step giving up on it is the behavior under test.
    /// </summary>
    readonly TaskCompletionSource<ObserverSubscriberResult> _neverAnswers = new();

    void Establish()
    {
        _observersConfig = new Observers { SubscriberTimeout = 1 };
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_neverAnswers.Task);
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    void Destroy() => _neverAnswers.TrySetCanceled();

    [Fact] void should_report_the_partition_as_failed_by_timeout() =>
        _observer.Received(1).PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            FailureKind.Timeout);
}
