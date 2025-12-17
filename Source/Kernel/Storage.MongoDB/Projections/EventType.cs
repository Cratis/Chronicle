// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents the type of an event.
/// </summary>
/// <param name="Id">The <see cref="EventTypeId"/>.</param>
/// <param name="Generation">The generation of the event type.</param>
public record EventType(EventTypeId Id, EventTypeGeneration Generation)
{
    /// <summary>
    /// Represents an unknown event type.
    /// </summary>
    public static readonly EventType Unknown = new(EventTypeId.Unknown, EventTypeGeneration.First);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id.Value}+{Generation.Value}";
    }

    /// <summary>
    /// Parse from a string representation of event type to actual type.
    /// </summary>
    /// <param name="input">String representation.</param>
    /// <returns>Parsed <see cref="EventType"/>.</returns>
    /// <remarks>
    /// The expected format is guid+generation.
    /// Ex: aa7faa25-afc1-48d1-8558-716581c0e916+1.
    /// </remarks>
    public static EventType Parse(string input)
    {
        var segments = input.Split('+');
        if (segments.Length == 1)
        {
            return new(segments[0], EventTypeGeneration.First);
        }

        return new(segments[0], uint.Parse(segments[1]));
    }
}
