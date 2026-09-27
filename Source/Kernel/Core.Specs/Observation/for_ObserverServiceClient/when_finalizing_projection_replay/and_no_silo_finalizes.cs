// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

public class and_no_silo_finalizes : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ObserverServiceClient.EnsureProjectionReplayFinalized([false, false]));

    [Fact] void should_fail_the_replay() => _exception.ShouldBeOfExactType<ReplayFinalizationFailed>();
}
