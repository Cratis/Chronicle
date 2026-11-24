// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="BehaviorPattern"/> and its <see cref="BehaviorPatternSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class BehaviorPatternSurrogateConverter : IConverter<BehaviorPattern, BehaviorPatternSurrogate>
{
    /// <inheritdoc/>
    public BehaviorPattern ConvertFromSurrogate(in BehaviorPatternSurrogate surrogate) => new(surrogate.GroupingKey, surrogate.Facets, surrogate.Occurrences, surrogate.Confidence, surrogate.Support, surrogate.Weight, surrogate.FirstSeen, surrogate.LastSeen);

    /// <inheritdoc/>
    public BehaviorPatternSurrogate ConvertToSurrogate(in BehaviorPattern value) => new(value.GroupingKey, value.Facets, value.Occurrences, value.Confidence, value.Support, value.Weight, value.FirstSeen, value.LastSeen);
}
