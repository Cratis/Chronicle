// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_reducer_finalization_fails : given.a_replay_observer_job
{
    readonly TaskCompletionSource _notified = new();

    void Establish()
    {
        _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.Status = JobStatus.Running;
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.SuccessfulSteps = 1;
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _observer.GetState().Returns(new ObserverState { LastHandledEventSequenceNumber = 12UL });
        _observer.Replayed(12UL).Returns(_ =>
        {
            _notified.SetResult();
            return Task.CompletedTask;
        });
        _replayServiceClient.EndReplayFor(Arg.Is<ObserverDetails>(details => !details.ReplayAborted))
            .Returns(Task.FromException(new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown)));
    }

    async Task Because()
    {
        await _job.Resume();
        await _notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_fail_the_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_preserve_the_published_recovery_position() => _observer.Received(1).Replayed(12UL);
    [Fact] void should_not_advance_past_unpublished_work() => _observer.DidNotReceive().Replayed(42UL);
    [Fact] void should_abandon_the_rebuild() => _replayServiceClient.Received(1).EndReplayFor(Arg.Is<ObserverDetails>(details => details.ReplayAborted));
    [Fact] void should_not_report_success() => _observer.DidNotReceive().ReplayedSuccessfullySince(
        Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
}
