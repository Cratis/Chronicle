// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that answers what usually happens in a given context.
/// </summary>
public interface IPatternMatcher
{
    /// <summary>
    /// Rank the patterns that apply to a context.
    /// </summary>
    /// <param name="patterns">The <see cref="BehaviorPattern">patterns</see> to consider.</param>
    /// <param name="context">The <see cref="FacetSet"/> describing the context, which may be partial.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> a match may hold.</param>
    /// <param name="maximumResults">The largest number of matches to return.</param>
    /// <returns>The matching patterns, most specific and most confident first.</returns>
    IEnumerable<BehaviorPattern> Match(
        IEnumerable<BehaviorPattern> patterns,
        FacetSet context,
        PatternConfidence minimumConfidence,
        int maximumResults);

    /// <summary>
    /// Rank the actions usually taken in a context, at most one result per action.
    /// </summary>
    /// <param name="patterns">The <see cref="BehaviorPattern">patterns</see> to consider.</param>
    /// <param name="context">The <see cref="FacetSet"/> describing the context, which may be partial.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> an answer may hold.</param>
    /// <param name="maximumResults">The largest number of answers to return.</param>
    /// <returns>The patterns naming what usually happens, most likely first.</returns>
    /// <remarks>
    /// The counterpart to <see cref="Match"/>: that one asks which established patterns describe a situation, this
    /// one asks what is usually done in it.
    /// </remarks>
    IEnumerable<BehaviorPattern> MatchActions(
        IEnumerable<BehaviorPattern> patterns,
        FacetSet context,
        PatternConfidence minimumConfidence,
        int maximumResults);
}
