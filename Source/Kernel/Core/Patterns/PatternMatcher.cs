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

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Confidence leads here, where specificity leads in <see cref="Match"/>, because the two rank different things.
    /// Matching ranks descriptions of a situation, and the one covering most of what was asked describes it best.
    /// This ranks answers, and confidence is already the chance of the action given the context it was established
    /// in - a number directly comparable between one answer and the next, which a facet count is not.
    /// </para>
    /// <para>
    /// One result per action. The same action is usually established at several context sizes at once - on a Monday,
    /// in the early morning, and on a Monday early morning - and returning all three says the same thing three times
    /// while pushing the second-most-likely action out of a limited result set. The survivor is the one conditioned
    /// on most of the question, which is the best-informed estimate of the three.
    /// </para>
    /// </remarks>
    public IEnumerable<BehaviorPattern> MatchActions(
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
                .Where(pattern => pattern.AnswersFor(context) && pattern.Confidence.Value >= minimumConfidence.Value)
                .GroupBy(pattern => pattern.Action)
                .Select(BestInformed)
                .OrderByDescending(pattern => pattern.Confidence.Value)
                .ThenByDescending(pattern => pattern.ContextSpecificity)
                .ThenByDescending(pattern => pattern.Support.Value)
                .ThenBy(pattern => pattern.Facets.Key.Value, StringComparer.Ordinal)
                .Take(maximumResults)
        ];
    }

    static BehaviorPattern BestInformed(IEnumerable<BehaviorPattern> forOneAction) =>
        forOneAction
            .OrderByDescending(pattern => pattern.ContextSpecificity)
            .ThenByDescending(pattern => pattern.Confidence.Value)
            .ThenByDescending(pattern => pattern.Support.Value)
            .ThenBy(pattern => pattern.Facets.Key.Value, StringComparer.Ordinal)
            .First();
}
