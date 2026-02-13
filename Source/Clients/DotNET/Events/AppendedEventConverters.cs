// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="AppendedEvent"/>.
/// </summary>
internal static class AppendedEventConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to convert.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
    /// <returns>Converted contract version.</returns>
    internal static Contracts.Events.AppendedEvent ToContract(this AppendedEvent @event, JsonSerializerOptions jsonSerializerOptions) => new()
    {
        Context = @event.Context.ToContract(),
        Content = JsonSerializer.Serialize(@event.Content, jsonSerializerOptions)
    };

    /// <summary>
    /// Convert to client version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
    /// <returns>Converted Chronicle version.</returns>
    internal static AppendedEvent ToClient(this Contracts.Events.AppendedEvent @event, IEventTypes eventTypes, JsonSerializerOptions jsonSerializerOptions)
    {
        var context = @event.Context.ToClient();
        var clrType = eventTypes.GetClrTypeFor(context.EventType.Id);
        var content = JsonSerializer.Deserialize(@event.Content, clrType, jsonSerializerOptions)!;

        return new(context, content);
    }

    /// <summary>
    /// Convert to a client version of a collection of <see cref="Contracts.Events.AppendedEvent"/>.
    /// </summary>
    /// <param name="events">Collection of <see cref="Contracts.Events.AppendedEvent"/> to convert from.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
    /// <returns>An immutable collection of <see cref="AppendedEvent"/>.</returns>
    internal static IImmutableList<AppendedEvent> ToClient(this IEnumerable<Contracts.Events.AppendedEvent> events, IEventTypes eventTypes, JsonSerializerOptions jsonSerializerOptions) =>
        events.Select(_ => _.ToClient(eventTypes, jsonSerializerOptions)).ToImmutableList();
}
