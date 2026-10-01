// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reducer_rebuild_succeeds : given.a_successful_reducer_replay
{
    async Task Because() => await _job.Resume();
    [Fact] void should_allow_an_intentionally_empty_reduction() => _reducerReplay.Received(1).Publish(Arg.Is<ReplayContext>(_ => _.AllowEmptyResult));
    [Fact]
    void should_advance_to_the_published_watermark() => _observer.Received(1).ReplayedFor(
        _jobId, 42UL, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), false);
    [Fact] void should_not_abandon_published_work() => _reducerReplay.DidNotReceive().Abandon(_jobId);
}
