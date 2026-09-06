// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the bounded set of contextual facts extracted from a single event before it is fed into pattern
/// mining.
/// </summary>
/// <param name="GroupingKey">The scope the behavior belongs to - typically the user that caused the event.</param>
/// <param name="CommandType">The type of command, or the event type when nothing above it named itself.</param>
/// <param name="InitiatorType">What kind of initiator caused the event.</param>
/// <param name="InitiatorId">The identifier of the initiator.</param>
/// <param name="OnBehalfOf">The identity the initiator acted on behalf of.</param>
/// <param name="CausedByCommand">The command one level up the causation chain.</param>
/// <param name="CorrelationRootId">The correlation the event belongs to.</param>
/// <param name="AggregateType">The type of the event source the event was appended to.</param>
/// <param name="Year">The year the event occurred in.</param>
/// <param name="Month">The month the event occurred in.</param>
/// <param name="Day">The day of week the event occurred on.</param>
/// <param name="TimeBucket">The part of the day the event occurred in.</param>
/// <param name="Occurred">When the event occurred, the source every time-derived value above was taken from.</param>
/// <remarks>
/// Features are derived per event and discarded once mined - nothing here is persisted. Every time-derived value
/// comes from the event's own occurred timestamp, never from wall-clock time at processing, so a backdated append
/// and a replay both land in the bucket the event actually belongs to.
/// </remarks>
public record EventFeatures(
    PatternGroupingKey GroupingKey,
    FacetValue CommandType,
    InitiatorType InitiatorType,
    FacetValue InitiatorId,
    FacetValue OnBehalfOf,
    FacetValue CausedByCommand,
    FacetValue CorrelationRootId,
    FacetValue AggregateType,
    int Year,
    int Month,
    DayOfWeek Day,
    TimeBucket TimeBucket,
    DateTimeOffset Occurred)
{
    /// <summary>
    /// Gets every facet the event carries, keyed by <see cref="FacetName"/>.
    /// </summary>
    /// <returns>A dictionary of every facet, including the ones holding no value.</returns>
    /// <remarks>
    /// Which of these take part in the mined itemset is a policy decision made further up, not here - this is the
    /// full vocabulary an event contributes, and the miner selects from it.
    /// </remarks>
    public IReadOnlyDictionary<FacetName, FacetValue> AsFacets() => new Dictionary<FacetName, FacetValue>
    {
        [FacetName.CommandType] = CommandType,
        [FacetName.InitiatorType] = new(InitiatorType.ToString()),
        [FacetName.InitiatorId] = InitiatorId,
        [FacetName.OnBehalfOf] = OnBehalfOf,
        [FacetName.CausedByCommand] = CausedByCommand,
        [FacetName.CorrelationRootId] = CorrelationRootId,
        [FacetName.AggregateType] = AggregateType,
        [FacetName.Year] = new(Year.ToString(CultureInfo.InvariantCulture)),
        [FacetName.Month] = new(Month.ToString(CultureInfo.InvariantCulture)),
        [FacetName.Day] = new(Day.ToString()),
        [FacetName.TimeBucket] = new(TimeBucket.ToString())
    };
}
