// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverService.when_finalizing_replay;

public class and_the_replay_context_was_already_evicted : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ObserverService.EnsureReplayFinalized(
        [Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle), Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext)]));

    [Fact] void should_not_fail_finalization_after_another_silo_evicted_it() => _exception.ShouldBeNull();
}
