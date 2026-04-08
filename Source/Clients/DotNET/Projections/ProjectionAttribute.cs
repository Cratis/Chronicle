// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Optional attribute used to adorn classes to configure a projection. The projection will have to implement <see cref="IProjectionFor{TReadModel}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ProjectionAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier. If not specified, it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. When not specified, the event sequence is inferred from the event types used in the projection definition.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ProjectionAttribute(string id = "", string? eventSequence = default) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the reducer.
    /// </summary>
    public ProjectionId Id { get; } = id;

    /// <summary>
    /// Gets the explicit event sequence identifier, or <see langword="null"/> if not specified.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the event sequence is inferred from the event types used in the projection definition.
    /// If all event types originate from the same event store (via <see cref="Cratis.Chronicle.Events.EventStoreAttribute"/>),
    /// the projection will automatically subscribe to the corresponding inbox event sequence.
    /// </remarks>
    public EventSequenceId? EventSequenceId { get; } = eventSequence is null ? null : new EventSequenceId(eventSequence);
}
