// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_abandoning_replay;

public class and_another_silo_already_abandoned_it : given.a_reducer_replay_handler
{
    void Establish() => _replayContexts.TryGet(_readModelIdentifier).Returns(GetContextError.NotFound);

    Task Because() => _handler.AbandonReplayFor(_observerDetails);

    [Fact] void should_flush_into_the_replay_container() => _sink.Received(1).EndBulk();
    [Fact] void should_leave_replay_on_this_silo() => _sink.Received(1).LeaveReplay();
    [Fact] void should_not_promote_anything() => _sink.DidNotReceiveWithAnyArgs().EndReplay(default!);
}
