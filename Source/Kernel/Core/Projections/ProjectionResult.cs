// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of an <see cref="IImmediateProjection"/>.
/// </summary>
/// <param name="ReadModel">The Json representation of the read model.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Projections.ProjectionResult")]
public record ProjectionResult([Id(0)] JsonObject ReadModel, [Id(1)] int ProjectedEventsCount, [Id(2)] EventSequenceNumber LastHandledEventSequenceNumber)
{
    /// <summary>
    /// Represents an empty <see cref="ProjectionResult"/>.
    /// </summary>
    public static readonly ProjectionResult Empty = new([], 0, EventSequenceNumber.Unavailable) { HasReadModel = false };

    /// <summary>
    /// Gets a value indicating whether the projection produced a read model.
    /// </summary>
    [Id(3)]
    public bool HasReadModel { get; init; } = true;
}
