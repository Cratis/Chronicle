// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_flushing_replay;

public class and_everything_held_back_is_written : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_succeed() => ErrorOf(_result).ShouldBeNull();
    [Fact] void should_flush_the_sink() => _sink.Received(1).EndBulk();
    [Fact] void should_not_leave_replay() => _sink.DidNotReceive().LeaveReplay();
}
