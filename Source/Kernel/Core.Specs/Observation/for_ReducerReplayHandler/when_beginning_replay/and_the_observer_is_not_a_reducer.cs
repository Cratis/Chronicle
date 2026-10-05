// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_beginning_replay;

public class and_the_observer_is_not_a_reducer : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.BeginReplayFor(_observerDetails with { Type = ObserverType.Projection });

    [Fact] void should_report_that_it_cannot_handle_it() => ErrorOf(_result).ShouldEqual(ICanHandleReplayForObserver.Error.CannotHandle);
    [Fact] void should_not_touch_the_sink() => _sink.DidNotReceiveWithAnyArgs().BeginReplay(default!);
}
