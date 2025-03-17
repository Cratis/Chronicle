// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers.for_LimitedConcurrencyLevelTaskScheduler;

public class when_provided_a_valid_max_concurrency_value : Specification
{
    static LimitedConcurrencyLevelTaskScheduler scheduler;
    const int maxConcurrency = 1;

    void Because() => scheduler = new LimitedConcurrencyLevelTaskScheduler(1, Substitute.For<IMeter<LimitedConcurrencyLevelTaskScheduler>>());

    [Fact] void should_contain_correct_concurrency_level() => scheduler.MaximumConcurrencyLevel.ShouldEqual(maxConcurrency);
}
