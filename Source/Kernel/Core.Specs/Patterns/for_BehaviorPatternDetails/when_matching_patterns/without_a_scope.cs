// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_BehaviorPatternDetails.when_matching_patterns;

/// <summary>
/// The scope selects whose behavior is being asked about. Without one there is no question to answer, so the
/// answer is nothing rather than everybody's patterns at once.
/// </summary>
public class without_a_scope : given.mined_patterns
{
    IEnumerable<BehaviorPatternDetails> _result;

    async Task Because() => _result = await BehaviorPatternDetails.MatchingPatterns(
        EventStore,
        EventStoreNamespaceName.Default,
        PatternGroupingKey.Unspecified,
        new Dictionary<string, string> { { FacetName.Day.Value, "Monday" } },
        _storage,
        _vocabulary,
        _generator,
        _matcher,
        _options);

    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();
}
