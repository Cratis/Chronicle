// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_removed;

public class and_reducer_partitions_are_incomplete : given.a_replay_observer_job
{
    readonly TaskCompletionSource _notified = new();

    void Establish()
    {
        _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.Status = JobStatus.Running;
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.SuccessfulSteps = 1;
        _stateStorage.State.Progress.StoppedSteps = 1;
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _observer.GetState().Returns(new ObserverState { LastHandledEventSequenceNumber = 12UL });
        _observer.Replayed(12UL).Returns(_ =>
        {
            _notified.SetResult();
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _job.Remove();
        await _notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_abandon_the_partial_rebuild() => _replayServiceClient.Received(1).EndReplayFor(Arg.Is<ObserverDetails>(details => details.ReplayAborted && !details.ReplaySucceededWithEvents));
    [Fact] void should_not_request_promotion() => _replayServiceClient.DidNotReceive().EndReplayFor(Arg.Is<ObserverDetails>(details => !details.ReplayAborted));
    [Fact] void should_preserve_the_published_recovery_position() => _observer.Received(1).Replayed(12UL);
    [Fact] void should_not_report_success() => _observer.DidNotReceive().ReplayedSuccessfullySince(
        Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
}
