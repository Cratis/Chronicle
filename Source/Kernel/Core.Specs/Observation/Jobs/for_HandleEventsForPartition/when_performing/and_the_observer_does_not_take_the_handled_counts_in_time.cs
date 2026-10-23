// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// The subscriber handled the events, but the observer was too busy to take the handled counts before the response
/// timeout (Cratis/Chronicle#4954). The step still succeeds, and sends the same report again so the counts are neither
/// lost nor taken twice.
/// </summary>
public class and_the_observer_does_not_take_the_handled_counts_in_time : given.a_performing_job_step
{
    Catch<JobStepResult> _result;
    int _reports;

    void Establish()
    {
        _jobStep.TransientRetryDelay = TimeSpan.Zero;
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(first_event_sequence_number)));
        _observer
            .ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<JobStepId>(), Arg.Any<EventSequenceNumber>())
            .Returns(_ => ++_reports == 1 ? Task.FromException(new TimeoutException("Response did not arrive on time")) : Task.CompletedTask);
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact]
    void should_succeed()
    {
        _result.TryGetResult(out var stepResult).ShouldBeTrue();
        stepResult.TryGetError(out _).ShouldBeFalse();
    }

    [Fact]
    void should_send_the_same_report_again() =>
        _observer.Received(2).ReportHandledEvents(
            Arg.Any<Key>(),
            Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts.Values.Sum(count => (long)count.Value) == 1),
            Arg.Any<JobStepId>(),
            first_event_sequence_number);

    [Fact]
    void should_send_it_with_the_same_identity_both_times() =>
        _observer.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents))
            .Select(call => call.GetArguments()[2])
            .Distinct()
            .Count()
            .ShouldEqual(1);

    [Fact]
    void should_not_report_the_partition_as_failed() =>
        _observer.DidNotReceive().PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<FailureKind>());
}
