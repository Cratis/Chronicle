// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class and_the_condition_never_becomes_ready : Cratis.Specifications.Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await ReadinessWait.Until(
        () => Task.FromResult(7), count => count == 9, TimeSpan.FromMilliseconds(100), "processed events"));

    [Fact] void should_fail_with_a_timeout() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_name_the_unmet_condition() => _exception.Message.ShouldContain("processed events");
    [Fact] void should_report_the_last_observation() => _exception.Message.ShouldContain("Last observed: 7");
}
