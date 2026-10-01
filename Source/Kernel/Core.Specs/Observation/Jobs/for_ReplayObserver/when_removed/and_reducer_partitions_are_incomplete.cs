// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_removed;

public class and_reducer_partitions_are_incomplete : given.a_successful_reducer_replay
{
    void Establish()
    {
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.StoppedSteps = 1;
    }
    async Task Because() => await _job.Remove();
    [Fact] void should_revoke_the_partial_rebuild() => _reducerReplay.Received().Abandon(_jobId);
    [Fact] void should_never_request_promotion() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
    [Fact]
    void should_preserve_the_published_position_with_an_identity_fenced_notification() => _observer.Received(1).ReplayedFor(
        _jobId, 42UL, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), true);
}
