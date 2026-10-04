// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_beginning_replay;

public class and_the_reducer_is_not_registered : given.a_reducer_replay_handler
{
    void Establish() => _reducers.Has(Arg.Any<ReducerId>()).Returns(false);

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_not_establish_a_replay_context() => _replayContexts.DidNotReceiveWithAnyArgs().Establish(default!, default!);
    [Fact] void should_not_touch_the_sink() => _sink.DidNotReceiveWithAnyArgs().BeginReplay(default!);
}
