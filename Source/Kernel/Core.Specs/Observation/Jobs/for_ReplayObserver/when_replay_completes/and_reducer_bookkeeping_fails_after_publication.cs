// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_reducer_bookkeeping_fails_after_publication : given.a_successful_reducer_replay
{
    void Establish() => _reducerReplay.Publish(Arg.Any<ReplayContext>()).Returns(ReplayPublication.PublishedWithBookkeepingFailure);
    async Task Because() => await _job.Resume();
    [Fact] void should_keep_the_failure_visible() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_retain_the_published_phase() => _stateStorage.State.ReducerReplayPhase.ShouldEqual(ReducerReplayPhase.Published);
    [Fact]
    void should_advance_even_if_the_published_result_is_empty() => _observer.Received(1).ReplayedFor(
        _jobId, 42UL, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), false);
    [Fact]
    void should_never_restore_the_old_position() => _observer.DidNotReceive().ReplayedFor(
        _jobId, Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), true);
}
