// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system for asking what usually happens, backed by the behavior patterns Chronicle mined from the
/// event history of an event store.
/// </summary>
public interface IPatterns
{
    /// <summary>
    /// Get the patterns that apply to a context, most specific and most confident first.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to ask within - typically a user.</param>
    /// <param name="context">The <see cref="FacetSet">context</see> to match, which may constrain any subset of the facets.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> an answer may hold. Defaults to the server's configured threshold.</param>
    /// <param name="maximumResults">The largest number of answers to return. Defaults to the server's default.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The matching <see cref="BehaviorPattern">patterns</see>, empty when nothing clears the bar.</returns>
    /// <remarks>
    /// An empty result is an answer, not a failure: it says this scope has no established behavior for this
    /// context. Nothing is invented to fill the gap, so a caller can treat "no patterns" as "do not claim to know".
    /// </remarks>
    Task<IEnumerable<BehaviorPattern>> GetPatterns(
        PatternGroupingKey groupingKey,
        FacetSet context,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get every pattern established for a scope.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to get for.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Every <see cref="BehaviorPattern"/> held for the scope.</returns>
    Task<IEnumerable<BehaviorPattern>> GetPatternsForScope(
        PatternGroupingKey groupingKey,
        CancellationToken cancellationToken = default);
}
