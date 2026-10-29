// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_a_failed_batch_is_retried : given.a_buffered_projection_job_step
{
    bool _firstAttemptFailed;
    bool _checkpointBeforeRetry;

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
        Task.FromResult(Windows(_window)), Task.FromResult(Windows()), Task.FromResult(Windows(_window)));

    async Task Because()
    {
        var first = await _jobStep.InvokePerformStep(_performState);
        _firstAttemptFailed = first.Match(result => result.TryGetError(out _), _ => true);
        _checkpointBeforeRetry = _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue;
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
        _result = await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_fail_the_first_attempt() => _firstAttemptFailed.ShouldBeTrue();
    [Fact] void should_not_checkpoint_before_retrying() => _checkpointBeforeRetry.ShouldBeFalse();
    [Fact] void should_redeliver_the_unsafe_batch() => _projection.Received(2).OnNext(
        Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == 1UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_succeed_on_retry() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
    [Fact] void should_checkpoint_the_retried_batch() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
}
