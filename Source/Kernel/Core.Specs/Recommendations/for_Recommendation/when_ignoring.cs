// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_Recommendation;

/// <summary>
/// The state is kept rather than cleared: clearing it only removed the row, and the evaluation that
/// raised the recommendation is unchanged, so the next client reconnect raised it again (#4141).
/// </summary>
public class when_ignoring : given.all_dependencies
{
    static given.TheRequest _request;
    static Exception _error;

    async Task Establish()
    {
        _request = new given.TheRequest(42);
        await recommendation.Initialize(description, _request);
        storageStats.ResetCounts();
    }

    async Task Because() => _error = await Catch.Exception(recommendation.Ignore);

    [Fact] void should_not_fail() => _error.ShouldBeNull();

    [Fact] void should_write_state_once() => storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_clear_state() => storageStats.Clears.ShouldEqual(0);
}
