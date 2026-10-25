// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_cursor_batch_holds_more_events_than_one_call_may_carry : given.a_performing_job_step
{
    readonly List<int> _callSizes = [];

    void Establish()
    {
        _jobStep.MaxEventsPerBatch = 2;
        _eventCursor.Current.Returns(Enumerable.Range(1, 5).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray());
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _callSizes.Add(events.Length);
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    Cratis.Monads.Catch<JobStepResult> _result;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_call_the_subscriber_once_per_slice() => string.Join(", ", _callSizes).ShouldEqual("2, 2, 1");

    [Fact]
    void should_report_the_handled_events_after_each_slice() => _observer.Received(3).ReportHandledEvents(
        Arg.Any<Key>(),
        Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(),
        Arg.Any<JobStepId>(),
        Arg.Any<EventSequenceNumber>());

    [Fact]
    void should_report_the_last_slice_as_handled_up_to_the_fifth_event() => _observer.Received(1).ReportHandledEvents(
        Arg.Any<Key>(),
        Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(),
        Arg.Any<JobStepId>(),
        (EventSequenceNumber)5UL);
}
