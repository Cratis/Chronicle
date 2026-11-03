// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_RecommendationDetails.when_observing;

public class and_states_are_mixed : given.all_dependencies
{
    void Because() => _source.OnNext([_active, _ignored]);

    [Fact] void should_emit_once_for_each_observable() => (_activeEmissions.Count, _ignoredEmissions.Count).ShouldEqual((1, 1));
    [Fact] void should_only_include_active_in_the_active_observable() => Ids(_activeEmissions[0]).ShouldContainOnly([_active.Id.Value]);
    [Fact] void should_only_include_ignored_in_the_ignored_observable() => Ids(_ignoredEmissions[0]).ShouldContainOnly([_ignored.Id.Value]);
}
