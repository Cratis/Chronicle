// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to mark a type as a read model with attribute-based projection configuration.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReadModelAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier. If not specified, it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class ReadModelAttribute(string id = "", string? eventSequence = default) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the projection.
    /// </summary>
    public ProjectionId Id { get; } = id;

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequence ?? EventSequenceId.Log;
}
