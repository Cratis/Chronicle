// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_another_silo_already_promoted_it : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _replayContexts.TryGet(_readModelIdentifier).Returns(GetContextError.NotFound);

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_report_that_there_was_no_context() => ErrorOf(_result).ShouldEqual(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext);
    [Fact] void should_not_promote_anything() => _sink.DidNotReceiveWithAnyArgs().EndReplay(default!);
    [Fact] void should_leave_replay_on_this_silo() => _sink.Received(1).LeaveReplay();
}
