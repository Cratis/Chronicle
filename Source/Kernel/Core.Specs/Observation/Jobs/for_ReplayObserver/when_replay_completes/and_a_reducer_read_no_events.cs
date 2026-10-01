// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reducer_read_no_events : given.a_successful_reducer_replay
{
    void Establish() => _stateStorage.State.LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
    async Task Because() => await _job.Resume();
    [Fact] void should_not_replace_the_existing_model_with_unproven_empty_state() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
    [Fact]
    void should_preserve_the_existing_position() => _observer.Received(1).ReplayedFor(
        _jobId, EventSequenceNumber.Unavailable, Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>(), true);
}
