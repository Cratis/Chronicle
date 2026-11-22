// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class and_the_condition_becomes_ready : Cratis.Specifications.Specification
{
    int _observations;
    int _result;

    async Task Because() => _result = await ReadinessWait.Until(
        () => Task.FromResult(++_observations), count => count == 3);

    [Fact] void should_return_the_ready_observation() => _result.ShouldEqual(3);
    [Fact] void should_stop_observing_when_ready() => _observations.ShouldEqual(3);
}
