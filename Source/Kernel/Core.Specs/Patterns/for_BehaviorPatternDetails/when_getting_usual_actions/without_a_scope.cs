// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_BehaviorPatternDetails.when_getting_usual_actions;

/// <summary>
/// The scope selects whose behavior is being asked about. Without one there is no question to answer, and the read
/// this query does is per scope - so it must be turned away before it reads rather than after.
/// </summary>
public class without_a_scope : given.mined_patterns
{
    IEnumerable<BehaviorPatternDetails> _result;

    async Task Because() => _result = await BehaviorPatternDetails.UsualActions(
        EventStore,
        EventStoreNamespaceName.Default,
        PatternGroupingKey.Unspecified,
        new Dictionary<string, string> { { FacetName.Day.Value, "Monday" } },
        _storage,
        _vocabulary,
        _matcher,
        _options);

    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();

    [Fact] async Task should_not_read_anything() =>
        await _patterns.DidNotReceive().GetForScope(Arg.Any<PatternGroupingKey>());
}
