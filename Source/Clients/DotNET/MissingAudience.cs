// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Exception that gets thrown when an event type marked with <see cref="AudienceAttribute"/> is appended without an audience.
/// </summary>
/// <param name="eventType">The CLR type of the event.</param>
public class MissingAudience(Type eventType) : Exception($"The event type '{eventType.FullName}' requires an audience but none was provided. Mark a property with [Audience], open EventTags.Audience(...), or tag the append with the '{WellKnownTags.Audience}' named tag.")
{
    /// <summary>
    /// Gets the CLR type of the event that was missing an audience.
    /// </summary>
    public Type EventType { get; } = eventType;
}
