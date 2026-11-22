// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class and_the_observation_fails : Cratis.Specifications.Specification
{
    readonly Exception _failure = new InvalidOperationException("Observation failed");
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await ReadinessWait.Until(
        () => Task.FromException<int>(_failure), count => count == 9));

    [Fact] void should_preserve_the_observation_failure() => _exception.ShouldEqual(_failure);
}
