// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json.Nodes;

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
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <returns>Converted contract version.</returns>
    internal static async Task<Contracts.Events.AppendedEvent> ToContract(this AppendedEvent @event, IEventSerializer eventSerializer) => new()
    {
        Metadata = @event.Metadata.ToContract(),
        Context = @event.Context.ToContract(),
        Content = (await eventSerializer.Serialize(@event.Content)).ToJsonString()
    };

    /// <summary>
    /// Convert to client version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for deserializing events.</param>
    /// <returns>Converted Chronicle version.</returns>
    internal static async Task<AppendedEvent> ToClient(this Contracts.Events.AppendedEvent @event, IEventSerializer eventSerializer) => new(
            @event.Metadata.ToClient(),
            @event.Context.ToClient(),
            await eventSerializer.Deserialize(typeof(ExpandoObject), JsonNode.Parse(@event.Content)!.AsObject()) as ExpandoObject ?? new ExpandoObject());

    /// <summary>
    /// Convert to a client version of a collection of <see cref="Contracts.Events.AppendedEvent"/>.
    /// </summary>
    /// <param name="events">Collection of <see cref="Contracts.Events.AppendedEvent"/> to convert from.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for deserializing events.</param>
    /// <returns>An immutable collection of <see cref="AppendedEvent"/>.</returns>
    internal static async Task<IImmutableList<AppendedEvent>> ToClient(this IEnumerable<Contracts.Events.AppendedEvent> events, IEventSerializer eventSerializer)
    {
        var tasks = events.Select(@event => @event.ToClient(eventSerializer));
        var convertedEvents = await Task.WhenAll(tasks);
        return convertedEvents.ToImmutableList();
    }
}
