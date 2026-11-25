// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class and_the_observation_never_completes : Cratis.Specifications.Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await ReadinessWait.Until(
        () => new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task,
        count => count == 9,
        TimeSpan.FromMilliseconds(100)));

    [Fact] void should_bound_the_observation_itself() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_report_that_no_observation_completed() => _exception.Message.ShouldContain("<no observation completed>");
}
