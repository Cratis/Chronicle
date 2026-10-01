// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_another_silo_attempted_finalization : given.a_reducer_replay_handler
{
    void Establish() => _details = _details with { ReplayAlreadyFinalized = true };
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_leave_replay() => _sink.Received(1).LeaveReplay();
    [Fact] void should_not_promote_again() => _pipeline.DidNotReceiveWithAnyArgs().EndReplay(default!);
    [Fact] void should_not_evict_a_context_the_finalizing_silo_retained_after_failure() => _contexts.DidNotReceiveWithAnyArgs().Evict(default!);
    [Fact] void should_notify_clients_of_end() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
