// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_an_end_notification_fails_after_publication : given.a_successful_reducer_replay
{
    void Establish() => _replayServiceClient.EndReplayFor(Arg.Any<ObserverDetails>()).Returns(Task.FromException(new IOException("silo unavailable")));
    async Task Because() => await _job.Resume();
    [Fact] void should_record_the_notification_failure() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact]
    void should_still_use_the_published_watermark() => _observer.Received(1).ReplayedFor(
        _jobId, 42UL, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), false);
    [Fact] void should_not_abandon_a_committed_swap() => _reducerReplay.DidNotReceive().Abandon(_jobId);
    [Fact] void should_not_require_silo_local_flush_rpcs_for_durably_acknowledged_batches() => _replayServiceClient.DidNotReceive().FlushReplayFor(Arg.Any<ObserverDetails>());
}
