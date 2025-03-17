// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Metrics;

namespace Cratis.Chronicle.Grains.Workers.for_LimitedConcurrencyLevelTaskScheduler;

public class when_provided_a_negative_max_concurrency_value : Specification
{
    static LimitedConcurrencyLevelTaskScheduler scheduler;

    static Exception error;

    void Because() => error = Catch.Exception(() => scheduler = new LimitedConcurrencyLevelTaskScheduler(-1, Substitute.For<IMeter<LimitedConcurrencyLevelTaskScheduler>>()));

    [Fact] void should_fail_because_invalid_maximum_concurrency_level() => error.ShouldBeOfExactType<InvalidMaximumConcurrencyLevel>();
}
