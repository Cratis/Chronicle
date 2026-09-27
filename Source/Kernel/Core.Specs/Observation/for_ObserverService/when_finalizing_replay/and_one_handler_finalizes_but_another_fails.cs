// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_finalizing_replay;

public class and_one_handler_finalizes_but_another_fails : Specification
{
    Exception _exception;

    void Because() => _exception = Cratis.Specifications.Catch.Exception(() => ObserverService.EnsureReplayFinalized(
        [Result<ICanHandleReplayForObserver.Error>.Success(), Result.Failed(ICanHandleReplayForObserver.Error.Unknown)]));

    [Fact] void should_fail_the_replay_despite_the_successful_handler() => _exception.ShouldBeOfExactType<ReplayFinalizationFailed>();
}
