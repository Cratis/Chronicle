// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="EventFeatures"/> and its <see cref="EventFeaturesSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class EventFeaturesSurrogateConverter : IConverter<EventFeatures, EventFeaturesSurrogate>
{
    /// <inheritdoc/>
    public EventFeatures ConvertFromSurrogate(in EventFeaturesSurrogate surrogate) => new(surrogate.GroupingKey, surrogate.CommandType, surrogate.InitiatorType, surrogate.InitiatorId, surrogate.OnBehalfOf, surrogate.CausedByCommand, surrogate.CorrelationRootId, surrogate.AggregateType, surrogate.Year, surrogate.Month, surrogate.Day, surrogate.TimeBucket, surrogate.Occurred);

    /// <inheritdoc/>
    public EventFeaturesSurrogate ConvertToSurrogate(in EventFeatures value) => new(value.GroupingKey, value.CommandType, value.InitiatorType, value.InitiatorId, value.OnBehalfOf, value.CausedByCommand, value.CorrelationRootId, value.AggregateType, value.Year, value.Month, value.Day, value.TimeBucket, value.Occurred);
}
