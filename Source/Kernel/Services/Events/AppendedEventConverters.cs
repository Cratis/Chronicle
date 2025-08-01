// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Json;

namespace Cratis.Chronicle.Services.Events;

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
    public static Contracts.Events.AppendedEvent ToContract(this AppendedEvent @event) => new()
    {
        Context = @event.Context.ToContract(),
        Content = JsonSerializer.Serialize(@event.Content, Globals.JsonSerializerOptions)
    };

    /// <summary>
    /// Convert a collection of <see cref="AppendedEvent"/> to a collection of <see cref="Contracts.Events.AppendedEvent"/>.
    /// </summary>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Contracts.Events.AppendedEvent"/>.</returns>
    public static IEnumerable<Contracts.Events.AppendedEvent> ToContract(this IEnumerable<AppendedEvent> events) => events.Select(ToContract);

    /// <summary>
    /// Convert to Chronicle version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static AppendedEvent ToChronicle(this Contracts.Events.AppendedEvent @event) => new(
            @event.Context.ToChronicle(),
            JsonSerializer.Deserialize<ExpandoObject>(@event.Content, Globals.JsonSerializerOptions)!);
}
