// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_Recommendation;

public class when_performing_while_ignored : given.all_dependencies
{
    static Exception _error;

    async Task Establish()
    {
        await recommendation.Initialize(description, new given.TheRequest(42));
        await recommendation.Ignore();
        storageStats.ResetCounts();
    }

    async Task Because() => _error = await Catch.Exception(recommendation.Perform);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_clear_state_once() => storageStats.Clears.ShouldEqual(1);
    [Fact] void should_not_write_state() => storageStats.Writes.ShouldEqual(0);
}
