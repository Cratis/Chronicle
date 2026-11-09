// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_beginning_replay;

public class and_the_projection_is_not_found : Specification
{
    Exception _exception;

    void Because() => _exception = Cratis.Specifications.Catch.Exception(() => ObserverService.EnsureReplayStarted(
        [Result.Failed(ICanHandleReplayForObserver.Error.ProjectionNotFound)]));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<ReplayInitializationFailed>();
    [Fact] void should_carry_the_cause() => ((ReplayInitializationFailed)_exception).Error.ShouldEqual(ICanHandleReplayForObserver.Error.ProjectionNotFound);
    [Fact] void should_name_the_cause_in_the_message() => _exception.Message.ShouldContain(nameof(ICanHandleReplayForObserver.Error.ProjectionNotFound));
}
