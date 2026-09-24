// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_Recommendation;

public class when_unignoring : given.all_dependencies
{
    static given.TheRequest _request;
    static Exception _error;

    async Task Establish()
    {
        _request = new given.TheRequest(42);
        await recommendation.Initialize(description, _request);
        await recommendation.Ignore();
        storageStats.ResetCounts();
    }

    async Task Because() => _error = await Catch.Exception(recommendation.Unignore);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_write_state_once() => storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_clear_state() => storageStats.Clears.ShouldEqual(0);
}
