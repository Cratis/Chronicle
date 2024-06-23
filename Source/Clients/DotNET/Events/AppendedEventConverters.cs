// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="AppendedEvent"/>.
/// </summary>
public static class AppendedEventConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Events.AppendedEvent ToContract(this AppendedEvent @event) => new()
    {
        Metadata = @event.Metadata.ToContract(),
        Context = @event.Context.ToContract(),
        Content = JsonSerializer.Serialize(@event.Content, Globals.JsonSerializerOptions)
    };

    /// <summary>
    /// Convert to Chronicle version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static AppendedEvent ToClient(this Contracts.Events.AppendedEvent @event) => new(
            @event.Metadata.ToClient(),
            @event.Context.ToClient(),
            JsonSerializer.Deserialize<ExpandoObject>(@event.Content, Globals.JsonSerializerOptions)!);
}
