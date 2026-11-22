// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="BehaviorPattern"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="GroupingKey">The <see cref="GroupingKey"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="Facets">The <see cref="Facets"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="Occurrences">The <see cref="Occurrences"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="Confidence">The <see cref="Confidence"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="Support">The <see cref="Support"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="Weight">The <see cref="Weight"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="FirstSeen">The <see cref="FirstSeen"/> member of the <see cref="BehaviorPattern"/>.</param>
/// <param name="LastSeen">The <see cref="LastSeen"/> member of the <see cref="BehaviorPattern"/>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.BehaviorPatternSurrogate")]
public readonly record struct BehaviorPatternSurrogate(
    [property: Id(0)] PatternGroupingKey GroupingKey,
    [property: Id(1)] FacetSet Facets,
    [property: Id(2)] PatternOccurrences Occurrences,
    [property: Id(3)] PatternConfidence Confidence,
    [property: Id(4)] PatternSupport Support,
    [property: Id(5)] PatternWeight Weight,
    [property: Id(6)] DateTimeOffset FirstSeen,
    [property: Id(7)] DateTimeOffset LastSeen);
