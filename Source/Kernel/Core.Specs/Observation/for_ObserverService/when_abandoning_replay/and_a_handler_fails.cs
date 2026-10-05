// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_abandoning_replay;

public class and_a_handler_fails : Specification
{
    Exception _exception;

    void Because() => _exception = Cratis.Specifications.Catch.Exception(() => ObserverService.EnsureReplayAbandoned(
        [Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle), Result.Failed(ICanHandleReplayForObserver.Error.Unknown)]));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<ReplayFinalizationFailed>();
}
