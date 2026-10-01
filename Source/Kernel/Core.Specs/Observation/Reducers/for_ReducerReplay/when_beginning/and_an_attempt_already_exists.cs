// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_beginning;

public class and_an_attempt_already_exists : given.a_reducer_replay
{
    ReplayContext _next;
    async Task Because() => _next = await _replay.Begin(_jobId);
    [Fact] void should_start_a_fresh_target_even_for_the_same_job() => (_next.ReplayContainerName == _context.ReplayContainerName).ShouldBeFalse();
    [Fact] void should_fence_the_previous_attempt() => _current!.RevertContainerName.ShouldEqual(_next.RevertContainerName);
    [Fact] void should_not_switch_the_live_sink_to_replay_mode() => _live.DidNotReceive().BeginReplay(Arg.Any<ReplayContext>());
    [Fact] void should_persist_one_shared_identity() => _contexts.Received(1).Save(_next);
    [Fact] void should_keep_names_within_postgresql_identifier_limits() => _next.ReplayContainerName!.Value.Length.ShouldBeLessThan(64);
}
