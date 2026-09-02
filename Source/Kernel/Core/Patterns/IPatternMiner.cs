// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines the grain that mines recurring behavior from the events of one event store namespace.
/// </summary>
/// <remarks>
/// The grain is keyed by <see cref="PatternMinerKey"/> - one activation per event store and namespace, guaranteed
/// by Orleans. That identity is what isolates behavior: the same scope name in two stores or two tenants'
/// namespaces resolves to two different grains, so their counts can never contaminate each other. The activation
/// owns the sketches, restores a scope's established patterns before its first mine, and persists what mining
/// touches on its own cadence.
/// </remarks>
public interface IPatternMiner : IGrainWithStringKey
{
    /// <summary>
    /// Mine the facts extracted from a batch of events.
    /// </summary>
    /// <param name="features">The <see cref="EventFeatures"/> for each event in the batch.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A scope acting for the first time in this activation's life has its established patterns restored before
    /// anything is mined for it. When that restore cannot be read, the call fails with nothing mined - so a
    /// redelivered batch counts nothing twice.
    /// </remarks>
    Task Mine(IEnumerable<EventFeatures> features);

    /// <summary>
    /// Gets every itemset that currently clears the support and confidence thresholds for one scope.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to get for.</param>
    /// <returns>The surviving <see cref="BehaviorPattern">patterns</see>.</returns>
    Task<IEnumerable<BehaviorPattern>> GetSurvivingPatterns(PatternGroupingKey groupingKey);

    /// <summary>
    /// Persist the scopes mining has touched since the last flush.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Runs on the grain's own cadence and on deactivation; exposed so a caller that needs the flush to have
    /// happened - an operator, a test - can force it rather than wait for the interval.
    /// </remarks>
    Task Persist();
}
