// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that mines recurring behavior from a stream of events.
/// </summary>
public interface IPatternMiner
{
    /// <summary>
    /// Mine the facts extracted from one event.
    /// </summary>
    /// <param name="features">The <see cref="EventFeatures"/> to mine.</param>
    void Observe(EventFeatures features);

    /// <summary>
    /// Decay every mined itemset as of a point in time.
    /// </summary>
    /// <param name="asOf"><see cref="DateTimeOffset">When</see> to decay as of.</param>
    void Decay(DateTimeOffset asOf);

    /// <summary>
    /// Gets every itemset that currently clears the support and confidence thresholds, across all scopes.
    /// </summary>
    /// <returns>The surviving <see cref="BehaviorPattern">patterns</see>.</returns>
    IEnumerable<BehaviorPattern> GetSurvivingPatterns();

    /// <summary>
    /// Gets every itemset that currently clears the support and confidence thresholds for one scope.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to get for.</param>
    /// <returns>The surviving <see cref="BehaviorPattern">patterns</see>.</returns>
    IEnumerable<BehaviorPattern> GetSurvivingPatterns(PatternGroupingKey groupingKey);
}
