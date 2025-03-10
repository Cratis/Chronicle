// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Recommendations.for_Recommendation;

public class when_ignoring : given.all_dependencies
{
    static given.TheRequest request;
    static Exception error;

    async Task Establish()
    {
        request = new given.TheRequest(42);
        await recommendation.Initialize(description, request);
        storageStats.ResetCounts();
    }

    async Task Because() => error = await Catch.Exception(recommendation.Ignore);

    [Fact] void should_not_fail() => error.ShouldBeNull();
    [Fact] void should_not_write_state() => storageStats.Writes.ShouldEqual(0);
    [Fact] void should_clear_state_once() => storageStats.Clears.ShouldEqual(1);
}
