// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_legacy_reducer_replay_has_zero_steps : given.a_successful_reducer_replay
{
    void Establish()
    {
        _stateStorage.State.Progress.TotalSteps = 0;
        _stateStorage.State.Progress.SuccessfulSteps = 0;
        _stateStorage.State.ReducerReplayContext = null;
    }
    async Task Because() => await _job.Resume();
    [Fact] void should_not_fail() => (_stateStorage.State.Status == JobStatus.Failed).ShouldBeFalse();
    [Fact] void should_not_finalize_any_context() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
    [Fact] void should_not_evict_another_replays_context() => _reducerReplay.DidNotReceive().Abandon(Arg.Any<JobId>());
    [Fact]
    void should_preserve_the_published_position() => _observer.Received(1).ReplayedFor(
        _jobId, Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), true);
}
