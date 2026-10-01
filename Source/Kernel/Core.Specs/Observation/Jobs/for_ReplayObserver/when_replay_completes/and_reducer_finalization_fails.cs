// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_reducer_finalization_fails : given.a_successful_reducer_replay
{
    void Establish() => _reducerReplay.Publish(Arg.Any<ReplayContext>()).Returns(Task.FromException<Reducers.ReplayPublication>(new IOException("lost reply")));
    async Task Because() => await _job.Resume();
    [Fact] void should_fail_the_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_preserve_the_unknown_publication_phase() => _stateStorage.State.ReducerReplayPhase.ShouldEqual(ReducerReplayPhase.Publishing);
    [Fact] void should_not_abandon_a_potentially_published_model() => _reducerReplay.DidNotReceive().Abandon(_jobId);
    [Fact]
    void should_not_resume_observation_at_either_unproven_position() => _observer.DidNotReceive().ReplayedFor(
        Arg.Any<JobId>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), Arg.Any<bool>());
}
