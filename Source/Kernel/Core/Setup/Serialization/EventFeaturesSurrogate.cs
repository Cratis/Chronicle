// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="EventFeatures"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="GroupingKey">The <see cref="GroupingKey"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="CommandType">The <see cref="CommandType"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="InitiatorType">The <see cref="InitiatorType"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="InitiatorId">The <see cref="InitiatorId"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="OnBehalfOf">The <see cref="OnBehalfOf"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="CausedByCommand">The <see cref="CausedByCommand"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="CorrelationRootId">The <see cref="CorrelationRootId"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="AggregateType">The <see cref="AggregateType"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="Year">The <see cref="Year"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="Month">The <see cref="Month"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="Day">The <see cref="Day"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="TimeBucket">The <see cref="TimeBucket"/> member of the <see cref="EventFeatures"/>.</param>
/// <param name="Occurred">The <see cref="Occurred"/> member of the <see cref="EventFeatures"/>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.EventFeaturesSurrogate")]
public readonly record struct EventFeaturesSurrogate(
    [property: Id(0)] PatternGroupingKey GroupingKey,
    [property: Id(1)] FacetValue CommandType,
    [property: Id(2)] InitiatorType InitiatorType,
    [property: Id(3)] FacetValue InitiatorId,
    [property: Id(4)] FacetValue OnBehalfOf,
    [property: Id(5)] FacetValue CausedByCommand,
    [property: Id(6)] FacetValue CorrelationRootId,
    [property: Id(7)] FacetValue AggregateType,
    [property: Id(8)] int Year,
    [property: Id(9)] int Month,
    [property: Id(10)] DayOfWeek Day,
    [property: Id(11)] TimeBucket TimeBucket,
    [property: Id(12)] DateTimeOffset Occurred);
