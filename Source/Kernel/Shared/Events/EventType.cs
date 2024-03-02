// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the type of an event.
/// </summary>
/// <param name="Id"><see cref="EventTypeId">Unique identifier</see>.</param>
/// <param name="Generation"><see cref="EventGeneration">Generation</see> of the event.</param>
/// <param name="IsPublic">Whether or not the event type is considered a public event.</param>
public record EventType(EventTypeId Id, EventGeneration Generation, bool IsPublic = false)
{
    /// <summary>
    /// Represents an unknown event type.
    /// </summary>
    public static readonly EventType Unknown = new(EventTypeId.Unknown, EventGeneration.First);

    /// <summary>
    /// Implicitly convert from <see cref="EventType"/> to string.
    /// </summary>
    /// <param name="type">EventType to convert from.</param>
    public static implicit operator string(EventType type) => type.ToString();

    /// <summary>
    /// Implicitly convert from string to <see cref="EventType"/>.
    /// </summary>
    /// <param name="value">String representation.</param>
    public static implicit operator EventType(string value) => Parse(value);

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
            return new(segments[0], EventGeneration.First);
        }
        return new(segments[0], uint.Parse(segments[1]));
    }
}
