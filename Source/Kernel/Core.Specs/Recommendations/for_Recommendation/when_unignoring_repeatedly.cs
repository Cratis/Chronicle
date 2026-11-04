// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_Recommendation;

public class when_unignoring_repeatedly : given.all_dependencies
{
    static Exception _error;

    async Task Establish()
    {
        await recommendation.Initialize(description, new given.TheRequest(42));
        await recommendation.Ignore();
        await recommendation.Unignore();
    }

    async Task Because() => _error = await Catch.Exception(recommendation.Unignore);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_be_ignored() => storage.State.IsIgnored.ShouldBeFalse();
    [Fact] void should_not_clear_state() => storageStats.Clears.ShouldEqual(0);
}
