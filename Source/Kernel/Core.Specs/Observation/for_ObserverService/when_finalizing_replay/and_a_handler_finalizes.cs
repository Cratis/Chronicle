// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_finalizing_replay;

public class and_a_handler_finalizes : Specification
{
    bool _finalized;

    void Because() => _finalized = ObserverService.EnsureReplayFinalized(
        [Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle), Result<ICanHandleReplayForObserver.Error>.Success()]);

    [Fact] void should_report_that_the_silo_finalized_the_replay() => _finalized.ShouldBeTrue();
}
