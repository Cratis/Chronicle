// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_getting_matching;

public class candidates_for_a_scope : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _result;

    async Task Because() => _result = await _storage.GetMatching(
        "user-42",
        [_mondayMorning.Facets.Key, _monday.Facets.Key, new FacetSetKey("Day=Tuesday")]);

    [Fact] void should_return_the_candidates_it_holds() => _result.Count().ShouldEqual(2);
    [Fact] void should_return_the_more_specific_one() => _result.ShouldContain(_mondayMorning);
    [Fact] void should_return_the_broader_one() => _result.ShouldContain(_monday);
    [Fact] void should_not_return_a_candidate_it_does_not_hold() => _result.Any(_ => _.Facets.Key.Value == "Day=Tuesday").ShouldBeFalse();
    [Fact] void should_not_reach_into_another_scope() => _result.ShouldNotContain(_forSomebodyElse);
}
