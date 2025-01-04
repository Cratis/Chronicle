// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json;
using Cratis.Json;

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
    /// <returns>Converted contract version.</returns>
    internal static Contracts.Events.AppendedEvent ToContract(this AppendedEvent @event) => new()
    {
        Metadata = @event.Metadata.ToContract(),
        Context = @event.Context.ToContract(),
        Content = JsonSerializer.Serialize(@event.Content, Globals.JsonSerializerOptions)
    };

    /// <summary>
    /// Convert to client version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    internal static AppendedEvent ToClient(this Contracts.Events.AppendedEvent @event) => new(
            @event.Metadata.ToClient(),
            @event.Context.ToClient(),
            JsonSerializer.Deserialize<ExpandoObject>(@event.Content, Globals.JsonSerializerOptions)!);

    /// <summary>
    /// Convert to a client version of a collection of <see cref="Contracts.Events.AppendedEvent"/>.
    /// </summary>
    /// <param name="events">Collection of <see cref="Contracts.Events.AppendedEvent"/> to convert from.</param>
    /// <returns>An immutable collection of <see cref="AppendedEvent"/>.</returns>
    internal static IImmutableList<AppendedEvent> ToClient(this IEnumerable<Contracts.Events.AppendedEvent> events) =>
        events.Select(_ => _.ToClient()).ToImmutableList();
}
