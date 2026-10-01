// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_events_were_successfully_handled : given.a_reducer_replay_handler
{
    void Establish() => _details = _details with { ReplaySucceededWithEvents = true };
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_allow_an_intentionally_empty_rebuild() => _pipeline.Received(1).EndReplay(Arg.Is<ReplayContext>(context => context.AllowEmptyResult));
    [Fact] void should_record_replayed() => _manager.Received(1).Replayed(_details.Key.ObserverId, _context);
    [Fact] void should_evict_context() => _contexts.Received(1).Evict(_context.Type.Identifier);
    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_notify_clients_of_end() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
