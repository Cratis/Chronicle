// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_initialization_was_aborted : given.a_reducer_replay_handler
{
    void Establish() => _details = _details with { ReplayAborted = true };
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_leave_replay_without_promoting() => _sink.Received(1).LeaveReplay();
    [Fact] void should_not_promote_uninitialized_state() => _pipeline.DidNotReceiveWithAnyArgs().EndReplay(default!);
    [Fact] void should_not_record_replayed() => _manager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
    [Fact] void should_evict_aborted_context() => _contexts.Received(1).Evict(_context.Type.Identifier);
    [Fact] void should_notify_clients_of_end() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
