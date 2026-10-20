// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_recovery_starts_while_another_partition_completes : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _firstPartition = "first-partition";
    static readonly Key _secondPartition = "second-partition";

    readonly TaskCompletionSource<ObserverSubscriberResult> _firstSubscriber = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _secondSubscriber = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Key> _jobStarting = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseJobStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _jobStarts;

    void Establish()
    {
        _subscriber.OnNext(_firstPartition, Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _firstSubscriber.Task);
        _subscriber.OnNext(_secondPartition, Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _secondSubscriber.Task);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(async call =>
            {
                if (++_jobStarts > 1)
                {
                    return Result<JobId, StartJobError>.Failed(StartJobError.Unknown);
                }

                _jobStarting.SetResult(call.Arg<CatchUpObserverPartitionRequest>().Key);
                await _releaseJobStart.Task;
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
    }

    async Task Because()
    {
        var firstHandling = _observer.Handle(_firstPartition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        var secondHandling = _observer.Handle(_secondPartition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        var recovery = _observer.TransitionTo<CatchingUpInFlight>();
        var recovering = await _jobStarting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // Whichever partition starts recovery first, complete the other live Handle while that start awaits.
        var completingSubscriber = recovering == _firstPartition ? _secondSubscriber : _firstSubscriber;
        completingSubscriber.SetResult(ObserverSubscriberResult.Ok(42UL));
        await (recovering == _firstPartition ? secondHandling : firstHandling);

        // The second start fails, so the hook retains that partition's handed marker even though Handle removed it.
        _releaseJobStart.SetResult();
        await recovery;
        (recovering == _firstPartition ? _firstSubscriber : _secondSubscriber).SetResult(ObserverSubscriberResult.Ok(42UL));
        await Task.WhenAll(firstHandling, secondHandling);
    }

    [Fact] void should_not_resurrect_the_completed_partition_marker() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_attempt_both_recovery_jobs() => _jobStarts.ShouldEqual(2);
}
