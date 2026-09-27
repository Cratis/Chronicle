// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverService.when_finalizing_replay;

public class and_the_replay_context_was_already_evicted : Specification
{
    bool _finalized;

    void Because() => _finalized = ObserverService.EnsureReplayFinalized(
        [Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle), Cratis.Monads.Result.Failed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext)]);

    [Fact] void should_report_that_this_silo_did_not_finalize_the_replay() => _finalized.ShouldBeFalse();
}
