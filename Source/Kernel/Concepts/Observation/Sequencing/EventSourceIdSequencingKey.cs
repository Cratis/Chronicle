// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents a default event source id key, including text that requires a bounded digest form.
/// </summary>
public sealed record EventSourceIdSequencingKey : SequencingKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourceIdSequencingKey"/> class.
    /// </summary>
    /// <param name="eventSourceId">The original event source identity, including malformed or oversized text.</param>
    /// <exception cref="InvalidSequencingKey">The event source identity is missing.</exception>
    public EventSourceIdSequencingKey(EventSourceId eventSourceId)
    {
        if (eventSourceId is null || eventSourceId.Value is null)
        {
            throw new InvalidSequencingKey("An event source id is required.");
        }

        EventSourceId = new(eventSourceId.Value);
    }

    /// <summary>
    /// Gets the original event source identity for delivery and display.
    /// </summary>
    public EventSourceId EventSourceId { get; }
}
