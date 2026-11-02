// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Optional configuration for a fluent <see cref="IProjectionFor{TReadModel}"/> or a model-bound read model.
/// On a model-bound read model this attribute selects identity only; mapping attributes are still required for discovery.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ProjectionAttribute"/>.
/// </remarks>
/// <param name="id">Optional stable identifier. If absent or empty, defaults to the fully qualified type name. Retaining an identifier does not prevent definition evolution.</param>
/// <param name="eventSequence">Optional the name of the event sequence for a fluent projection. When not specified, the event sequence is inferred from the event types used in the projection definition. Model-bound read models use <see cref="EventSequenceAttribute"/> instead.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class ProjectionAttribute(string id = "", string? eventSequence = default) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the projection.
    /// </summary>
    public ProjectionId Id { get; } = id;

    /// <summary>
    /// Gets the explicit event sequence identifier, or <see langword="null"/> if not specified.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the event sequence is inferred from the event types used in the projection definition.
    /// If all event types originate from the same event store (via <see cref="Events.EventStoreAttribute"/>),
    /// the projection will automatically subscribe to the corresponding inbox event sequence.
    /// </remarks>
    public EventSequenceId? EventSequenceId { get; } = eventSequence is null ? null : new EventSequenceId(eventSequence);
}
