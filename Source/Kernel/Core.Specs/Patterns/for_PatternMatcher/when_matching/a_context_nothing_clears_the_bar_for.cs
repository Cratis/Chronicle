// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching;

/// <summary>
/// Nothing clearing the confidence bar returns nothing. An empty answer is a true statement about a context with
/// no established behavior; the best of a bad set reads to a caller exactly like a real pattern.
/// </summary>
public class a_context_nothing_clears_the_bar_for : Specification
{
    IEnumerable<BehaviorPattern> _result;

    void Because() => _result = new PatternMatcher().Match(
        [
            new BehaviorPattern(
                "user-42",
                new FacetSet([new Facet(FacetName.Day, "Monday")]),
                3,
                0.2d,
                0.1d,
                1d,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch)
        ],
        new FacetSet([new Facet(FacetName.Day, "Monday")]),
        new PatternConfidence(0.5d),
        10);

    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();
}
