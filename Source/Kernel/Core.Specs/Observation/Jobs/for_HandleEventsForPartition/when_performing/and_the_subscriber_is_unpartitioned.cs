// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// An unpartitioned subscriber is bounded at the size of its own bounded call, so each progress checkpoint matches
/// exactly one call.
/// </summary>
public class and_the_subscriber_is_unpartitioned : given.a_performing_job_step
{
    readonly List<ulong> _reportedUpTo = [];

    async Task Establish()
    {
        var unpartitioned = Substitute.For<given.IUnpartitionedObserverType>();
        unpartitioned.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Task.FromResult(ObserverSubscriberResult.Ok(call.ArgAt<IEnumerable<AppendedEvent>>(1).Last().Context.SequenceNumber)));
        _jobStep.SetupForTesting(_observer, unpartitioned, "some-partition", await _observer.GetSubscription());
        _eventCursor.Current.Returns(Enumerable.Range(1, 45).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray());
        _ = _observer.ReportHandledEvents(
            Arg.Any<Key>(),
            Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(),
            Arg.Any<JobStepId>(),
            Arg.Do<EventSequenceNumber>(_ => _reportedUpTo.Add(_.Value)));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_progress_per_bounded_call() => string.Join(", ", _reportedUpTo).ShouldEqual("20, 40, 45");
}
