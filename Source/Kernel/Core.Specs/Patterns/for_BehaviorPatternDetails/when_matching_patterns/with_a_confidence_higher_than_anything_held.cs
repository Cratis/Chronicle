// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_BehaviorPatternDetails.when_matching_patterns;

public class with_a_confidence_higher_than_anything_held : given.mined_patterns
{
    IEnumerable<BehaviorPatternDetails> _result;

    async Task Because() => _result = await BehaviorPatternDetails.MatchingPatterns(
        EventStore,
        EventStoreNamespaceName.Default,
        Scope,
        new Dictionary<string, string> { { FacetName.Day.Value, "Monday" } },
        _storage,
        _vocabulary,
        _generator,
        _matcher,
        _options,
        minimumConfidence: 0.95d);

    [Fact] void should_return_nothing_rather_than_the_best_of_a_bad_set() => _result.ShouldBeEmpty();
}
