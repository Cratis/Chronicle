// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventType"/>.
/// </summary>
public static class EventTypeConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventType"/>.
    /// </summary>
    /// <param name="type"><see cref="EventType"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Events.EventType ToContract(this EventType type)
    {
        return new()
        {
            Id = type.Id.Value.ToString(),
            Generation = type.Generation.Value,
            IsPublic = type.IsPublic
        };
    }

    /// <summary>
    /// Convert to a collection of contract version of <see cref="EventType"/>.
    /// </summary>
    /// <param name="types">Collection of <see cref="EventType"/> to convert.</param>
    /// <returns>Converted collection of contract version.</returns>
    public static IEnumerable<Contracts.Events.EventType> ToContract(this IEnumerable<EventType> types) =>
        types.Select(_ => _.ToContract()).ToArray();

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="eventType"><see cref="Contracts.Events.EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToChronicle(this Contracts.Events.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.IsPublic);

    /// <summary>
    /// Convert to a collection of kernel version of <see cref="Contracts.Events.EventType"/>.
    /// </summary>
    /// <param name="types">Collection of <see cref="Contracts.Events.EventType"/> to convert.</param>
    /// <returns>Converted collection of contract version.</returns>
    public static IEnumerable<EventType> ToChronicle(this IEnumerable<Contracts.Events.EventType> types) =>
        types.Select(_ => _.ToChronicle()).ToArray();
}
