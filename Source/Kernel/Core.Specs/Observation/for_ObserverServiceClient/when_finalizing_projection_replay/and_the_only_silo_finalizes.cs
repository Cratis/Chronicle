// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

public class and_the_only_silo_finalizes : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ObserverServiceClient.EnsureProjectionReplayFinalized([true]));

    [Fact] void should_accept_the_replay() => _exception.ShouldBeNull();
}
