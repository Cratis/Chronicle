// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class for_a_duration_and_the_observation_never_completes : Cratis.Specifications.Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await ReadinessWait.ObserveFor(
        () => new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task,
        TimeSpan.FromMilliseconds(100)));

    [Fact] void should_not_treat_an_unobserved_state_as_absent() => _exception.ShouldBeOfExactType<TimeoutException>();
}
