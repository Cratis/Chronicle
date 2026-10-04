// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_beginning_replay;

public class and_the_reducer_exists : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_succeed() => ErrorOf(_result).ShouldBeNull();
    [Fact] void should_apply_the_replay_retention_policy() => _replayManager.Received(1).ApplyRetentionPolicy(ReplayedVersionsToKeep);
    [Fact] void should_establish_a_replay_context_for_the_read_model() => _replayContexts.Received(1).Establish(new ReadModelType(_readModelIdentifier, ReadModelGeneration.First), _containerName);
    [Fact] void should_put_the_read_models_sink_into_replay_mode() => _sink.Received(1).BeginReplay(_replayContext);
    [Fact] void should_notify_connected_clients() => _reducerMediator.Received(1).OnBeginReplay(new ReducerId("TheReducer"), _observerDetails.Key.EventStore, _observerDetails.Key.Namespace);
}
