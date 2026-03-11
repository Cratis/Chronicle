// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Shared parser for event type filter strings.
/// </summary>
static class EventTypeParser
{
    /// <summary>
    /// Parses a comma-separated event type string into a list of contracts EventType instances.
    /// Each entry accepts "name" (defaults to generation 1) or "name+generation".
    /// </summary>
    /// <param name="input">Comma-separated event type string to parse.</param>
    /// <returns>A list of <see cref="EventType"/> instances.</returns>
    public static List<EventType> ParseEventTypes(string input) =>
        input
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseEventType)
            .ToList();

    /// <summary>
    /// Parses a single event type string into a contracts EventType.
    /// Accepts "name" (defaults to generation 1) or "name+generation".
    /// </summary>
    /// <param name="input">The event type string to parse.</param>
    /// <returns>A contracts <see cref="EventType"/> instance.</returns>
    public static EventType ParseEventType(string input)
    {
        var parts = input.Split('+');
        return new EventType
        {
            Id = parts[0],
            Generation = parts.Length > 1 && uint.TryParse(parts[1], out var gen) ? gen : 1u
        };
    }
}
