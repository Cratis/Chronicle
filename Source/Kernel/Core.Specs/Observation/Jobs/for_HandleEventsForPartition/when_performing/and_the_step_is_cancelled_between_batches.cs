// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_is_cancelled_between_batches : given.a_performing_job_step
{
    readonly CancellationTokenSource _cancellation = new();
    Catch<JobStepResult> _result;

    void Establish()
    {
        var moveNextCount = 0;
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(moveNextCount++ < 2));
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(async _ =>
            {
                await _cancellation.CancelAsync();
                return ObserverSubscriberResult.Ok(first_event_sequence_number);
            });
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState, _cancellation.Token);

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_keep_the_progress_made_before_cancelling() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(first_event_sequence_number);
    [Fact] void should_only_deliver_the_first_batch() => _observerSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
