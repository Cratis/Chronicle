// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_abandoning_replay;

public class and_the_context_exists : given.a_reducer_replay_handler
{
    readonly List<string> _calls = [];

    void Establish()
    {
        _sink.When(_ => _.EndBulk()).Do(_ => _calls.Add("flush"));
        _sink.When(_ => _.LeaveReplay()).Do(_ => _calls.Add("leave"));
    }

    Task Because() => _handler.AbandonReplayFor(_observerDetails);

    [Fact] void should_flush_what_is_held_back_into_the_replay_container_before_leaving_replay() => _calls.ShouldContainOnly(["flush", "leave"]);
    [Fact] void should_flush_before_leaving() => _calls[0].ShouldEqual("flush");
    [Fact] void should_not_promote_anything() => _sink.DidNotReceiveWithAnyArgs().EndReplay(default!);
    [Fact] void should_not_mark_the_read_model_as_replayed() => _replayManager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
    [Fact] void should_evict_the_replay_context() => _replayContexts.Received(1).Evict(_readModelIdentifier);
    [Fact] void should_notify_connected_clients() => _reducerMediator.Received(1).OnEndReplay(new ReducerId("TheReducer"), _observerDetails.Key.EventStore, _observerDetails.Key.Namespace);
}
