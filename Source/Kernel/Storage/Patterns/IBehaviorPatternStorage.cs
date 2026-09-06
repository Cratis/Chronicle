// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.Patterns;

/// <summary>
/// Defines a system for working with the <see cref="BehaviorPattern">patterns</see> that survived mining.
/// </summary>
/// <remarks>
/// Only surviving patterns reach storage, so what is held here scales with distinct recurring behavior rather than
/// with event volume. Nothing per-event is ever written.
/// </remarks>
public interface IBehaviorPatternStorage
{
    /// <summary>
    /// Save patterns, replacing any already held for the same scope and facet set.
    /// </summary>
    /// <param name="patterns">The <see cref="BehaviorPattern">patterns</see> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(IEnumerable<BehaviorPattern> patterns);

    /// <summary>
    /// Get every pattern held for a scope.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to get for.</param>
    /// <returns>The <see cref="BehaviorPattern">patterns</see> held for the scope.</returns>
    Task<IEnumerable<BehaviorPattern>> GetForScope(PatternGroupingKey groupingKey);

    /// <summary>
    /// Get the patterns of a scope whose facet set is one of a set of candidates.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to get for.</param>
    /// <param name="candidates">The candidate <see cref="FacetSetKey">facet set keys</see> to look up.</param>
    /// <returns>The <see cref="BehaviorPattern">patterns</see> matching any of the candidates.</returns>
    /// <remarks>
    /// A partial context expands to a bounded set of candidate keys, so answering "what usually happens here" is a
    /// keyed lookup rather than a scan over everything the scope has ever done.
    /// </remarks>
    Task<IEnumerable<BehaviorPattern>> GetMatching(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> candidates);

    /// <summary>
    /// Get every scope that holds patterns.
    /// </summary>
    /// <returns>The <see cref="PatternGroupingKey">scopes</see> held.</returns>
    Task<IEnumerable<PatternGroupingKey>> GetScopes();

    /// <summary>
    /// Remove every pattern held for a scope that is not among the given facet sets.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to remove within.</param>
    /// <param name="surviving">The <see cref="FacetSetKey">facet set keys</see> that should remain.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This is how a pattern that decayed below the threshold leaves storage. Mining reports what survives, not
    /// what died, so the pruning step is expressed as "keep only these" rather than as a list of removals.
    /// </remarks>
    Task RemoveAllExcept(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> surviving);
}
