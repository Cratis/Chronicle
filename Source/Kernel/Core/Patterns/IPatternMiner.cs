// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that mines recurring behavior from a stream of events.
/// </summary>
/// <remarks>
/// Every operation names the event store and namespace it works within. Behavior belongs to a scope <b>inside</b>
/// a store's namespace - the same scope name in two stores is two different people's behavior, and counting them
/// into one sketch would contaminate both.
/// </remarks>
public interface IPatternMiner
{
    /// <summary>
    /// Mine the facts extracted from one event.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the event belongs to.</param>
    /// <param name="features">The <see cref="EventFeatures"/> to mine.</param>
    void Observe(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventFeatures features);

    /// <summary>
    /// Seed a scope with the patterns an earlier life of the miner had established, unless the scope already
    /// holds live counts.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the scope belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the scope belongs to.</param>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to restore.</param>
    /// <param name="patterns">The <see cref="BehaviorPattern">patterns</see> that survived the earlier life.</param>
    void Restore(EventStoreName eventStore, EventStoreNamespaceName @namespace, PatternGroupingKey groupingKey, IEnumerable<BehaviorPattern> patterns);

    /// <summary>
    /// Decay every mined itemset as of a point in time.
    /// </summary>
    /// <param name="asOf"><see cref="DateTimeOffset">When</see> to decay as of.</param>
    void Decay(DateTimeOffset asOf);

    /// <summary>
    /// Gets every itemset that currently clears the support and confidence thresholds for one scope.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the scope belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the scope belongs to.</param>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to get for.</param>
    /// <returns>The surviving <see cref="BehaviorPattern">patterns</see>.</returns>
    IEnumerable<BehaviorPattern> GetSurvivingPatterns(EventStoreName eventStore, EventStoreNamespaceName @namespace, PatternGroupingKey groupingKey);
}
