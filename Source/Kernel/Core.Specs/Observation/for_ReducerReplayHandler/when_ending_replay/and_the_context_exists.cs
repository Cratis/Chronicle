// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_the_context_exists : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_succeed() => ErrorOf(_result).ShouldBeNull();
    [Fact] void should_promote_the_rebuilt_read_model() => _sink.Received(1).EndReplay(_replayContext);
    [Fact] void should_mark_the_read_model_as_replayed() => _replayManager.Received(1).Replayed(_observerDetails.Key.ObserverId, _replayContext);
    [Fact] void should_evict_the_replay_context() => _replayContexts.Received(1).Evict(_readModelIdentifier);
    [Fact] void should_notify_connected_clients() => _reducerMediator.Received(1).OnEndReplay(new ReducerId("TheReducer"), _observerDetails.Key.EventStore, _observerDetails.Key.Namespace);
}
