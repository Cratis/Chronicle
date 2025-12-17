// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the result of an <see cref="IImmediateProjection"/>.
/// </summary>
/// <param name="ReadModel">The Json representation of the read model.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ProjectionResult(JsonObject ReadModel, int ProjectedEventsCount, EventSequenceNumber LastHandledEventSequenceNumber)
{
    /// <summary>
    /// Represents an empty <see cref="ProjectionResult"/>.
    /// </summary>
    public static readonly ProjectionResult Empty = new([], 0, EventSequenceNumber.Unavailable);
}
