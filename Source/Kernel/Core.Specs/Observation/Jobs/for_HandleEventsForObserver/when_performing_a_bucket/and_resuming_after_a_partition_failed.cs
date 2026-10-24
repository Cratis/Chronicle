// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_resuming_after_a_partition_failed : given.a_bucket_with_a_failed_partition
{
    async Task Because()
    {
        var restored = ReplaySerialization.Restore(_persistedState);
        await ReestablishStep();
        _performState = restored;
        _stateStorage.State = restored;
        _eventCursor.Current.Returns([CreateEvent(3UL, "feature"), CreateEvent(4UL, "module")]);
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
        await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_have_interrupted_the_first_attempt() => _firstAttempt.Match(result => result.TryGetError(out var error) && error.HasPartialResult && !error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_restore_the_failed_partition() => _performState.BucketFailedPartitions.SequenceEqual([(Key)"feature"]).ShouldBeTrue();
    [Fact] void should_not_deliver_the_failed_partition_after_restart() => _observerSubscriber.DidNotReceive().OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_continue_with_the_healthy_partition() => _handledBatches.SelectMany(batch => batch.SequenceNumbers).SequenceEqual(new EventSequenceNumber[] { 2UL, 4UL }).ShouldBeTrue();
    [Fact] void should_resume_after_the_restored_checkpoint() => _startSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
