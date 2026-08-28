// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternMatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// Specificity outranks confidence. A pattern that constrains everything the caller asked about is an answer to
/// their question; a broader one that happens to be more confident is an answer to a question they did not ask, and
/// putting it first is how a recommendation engine ends up stating the obvious.
/// </para>
/// <para>
/// Nothing clearing the confidence bar returns nothing. An empty answer is a true statement about a context with no
/// established behavior, whereas the best of a bad set reads to a caller exactly like a real pattern.
/// </para>
/// </remarks>
[Singleton]
public class PatternMatcher : IPatternMatcher
{
    /// <inheritdoc/>
    public IEnumerable<BehaviorPattern> Match(
        IEnumerable<BehaviorPattern> patterns,
        FacetSet context,
        PatternConfidence minimumConfidence,
        int maximumResults)
    {
        if (maximumResults <= 0)
        {
            return [];
        }

        return
        [
            .. patterns
                .Where(pattern => pattern.Matches(context) && pattern.Confidence.Value >= minimumConfidence.Value)
                .OrderByDescending(pattern => pattern.Specificity)
                .ThenByDescending(pattern => pattern.Confidence.Value)
                .ThenByDescending(pattern => pattern.Support.Value)
                .ThenBy(pattern => pattern.Facets.Key.Value, StringComparer.Ordinal)
                .Take(maximumResults)
        ];
    }
}
