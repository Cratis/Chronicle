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
}
