// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverService.when_finalizing_replay;

public class and_a_handler_returns_an_error : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ObserverService.EnsureReplayFinalized(
        [Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle), Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.Unknown)]));

    [Fact] void should_not_treat_the_replay_as_finalized() => _exception.ShouldBeOfExactType<ReplayFinalizationFailed>();
}
