// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents a logical value or unresolved sequencing key.
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
        if (original is null || this is not ValueSequencingKey and not UnresolvedSequencingKey)
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
    /// Creates the default value key for an event source id without substituting malformed text.
    /// </summary>
    /// <param name="eventSourceId">The event source identity.</param>
    /// <returns>A single-part value key.</returns>
    /// <exception cref="InvalidSequencingKey">The event source identity is missing.</exception>
    public static ValueSequencingKey ForEventSourceId(EventSourceId eventSourceId)
    {
        if (eventSourceId is null || eventSourceId.Value is null)
        {
            throw new InvalidSequencingKey("An event source id is required.");
        }

        return new([eventSourceId.Value]);
    }

    /// <summary>
    /// Creates an unresolved key for any non-null event type id.
    /// </summary>
    /// <param name="eventType">The event type identity.</param>
    /// <returns>The unresolved logical key.</returns>
    public static UnresolvedSequencingKey Unresolved(EventTypeId eventType) => new(eventType);
}
