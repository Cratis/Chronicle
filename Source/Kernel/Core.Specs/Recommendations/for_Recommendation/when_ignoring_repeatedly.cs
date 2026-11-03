// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_Recommendation;

public class when_ignoring_repeatedly : given.all_dependencies
{
    static Exception _error;

    async Task Establish()
    {
        await recommendation.Initialize(description, new given.TheRequest(42));
        await recommendation.Ignore();
    }

    async Task Because() => _error = await Catch.Exception(recommendation.Ignore);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_still_be_ignored() => storage.State.IsIgnored.ShouldBeTrue();
    [Fact] void should_not_clear_state() => storageStats.Clears.ShouldEqual(0);
}
