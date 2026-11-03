// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_RecommendationDetails.when_observing;

public class and_a_recommendation_is_ignored_then_unignored : given.all_dependencies
{
    void Because()
    {
        _source.OnNext([_active]);
        _active.IsIgnored = true;
        _source.OnNext([_active]);
        _active.IsIgnored = false;
        _source.OnNext([_active]);
    }

    [Fact] void should_emit_for_every_change() => (_activeEmissions.Count, _ignoredEmissions.Count).ShouldEqual((3, 3));
    [Fact] void should_be_active_before_ignoring() => Ids(_activeEmissions[0]).ShouldContainOnly([_active.Id.Value]);
    [Fact] void should_not_be_ignored_before_ignoring() => Ids(_ignoredEmissions[0]).ShouldBeEmpty();
    [Fact] void should_leave_the_active_observable_when_ignored() => Ids(_activeEmissions[1]).ShouldBeEmpty();
    [Fact] void should_enter_the_ignored_observable_when_ignored() => Ids(_ignoredEmissions[1]).ShouldContainOnly([_active.Id.Value]);
    [Fact] void should_return_to_the_active_observable_when_unignored() => Ids(_activeEmissions[2]).ShouldContainOnly([_active.Id.Value]);
    [Fact] void should_leave_the_ignored_observable_when_unignored() => Ids(_ignoredEmissions[2]).ShouldBeEmpty();
}
