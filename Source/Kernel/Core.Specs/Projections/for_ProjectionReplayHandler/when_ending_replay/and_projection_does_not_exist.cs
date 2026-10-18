// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

public class and_projection_does_not_exist : given.a_projection_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_report_an_error() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_not_end_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().EndReplay(null!);
}
