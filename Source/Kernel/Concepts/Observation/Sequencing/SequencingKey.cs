// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents a logical event source id, value, or unresolved sequencing key.
/// </summary>
public abstract record SequencingKey
{
    /// <summary>
    /// Copies a logical key without permitting foreign record types to enter the hierarchy.
    /// </summary>
    /// <param name="original">The logical key being copied.</param>
    /// <exception cref="InvalidSequencingKey">A foreign record tries to derive from the closed hierarchy.</exception>
    protected SequencingKey(SequencingKey original)
    {
        if (original is null || this is not EventSourceIdSequencingKey and not ValueSequencingKey and not UnresolvedSequencingKey)
        {
            throw new InvalidSequencingKey("The sequencing key hierarchy is closed.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencingKey"/> class within the closed hierarchy.
    /// </summary>
    private protected SequencingKey()
    {
    }

    /// <summary>
    /// Creates a default event source id key, retaining its original text for delivery and display.
    /// </summary>
    /// <param name="eventSourceId">The event source identity.</param>
    /// <returns>A logical key whose canonical form is a Value key or a bounded event source id digest.</returns>
    /// <exception cref="InvalidSequencingKey">The event source identity is missing.</exception>
    public static EventSourceIdSequencingKey ForEventSourceId(EventSourceId eventSourceId) => new(eventSourceId);

    /// <summary>
    /// Creates an unresolved key for any non-null event type id.
    /// </summary>
    /// <param name="eventType">The event type identity.</param>
    /// <returns>The unresolved logical key.</returns>
    public static UnresolvedSequencingKey Unresolved(EventTypeId eventType) => new(eventType);
}
