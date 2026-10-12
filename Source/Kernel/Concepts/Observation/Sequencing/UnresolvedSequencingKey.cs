// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents events parked by their ordinal event type identity.
/// </summary>
public sealed record UnresolvedSequencingKey : SequencingKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnresolvedSequencingKey"/> class.
    /// </summary>
    /// <param name="eventType">The event type identity, including malformed or oversized text.</param>
    /// <exception cref="InvalidSequencingKey">The event type identity is missing.</exception>
    public UnresolvedSequencingKey(EventTypeId eventType)
    {
        if (eventType is null || eventType.Value is null)
        {
            throw new InvalidSequencingKey("An event type id is required.");
        }

        EventType = new(eventType.Value);
    }

    /// <summary>
    /// Gets the event type identity.
    /// </summary>
    public EventTypeId EventType { get; }
}
