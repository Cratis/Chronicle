// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Json;

namespace Aksio.Cratis.Events;

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
    public static Kernel.Contracts.Events.AppendedEvent ToContract(this AppendedEvent @event) => new()
    {
        Metadata = @event.Metadata.ToContract(),
        Context = @event.Context.ToContract(),
        Content = JsonSerializer.Serialize(@event.Content, Globals.JsonSerializerOptions)
    };

    /// <summary>
    /// Convert to kernel version of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Kernel.Contracts.Events.AppendedEvent"/> to convert.</param>
    /// <returns>Converted kernel version.</returns>
    public static AppendedEvent ToKernel(this Kernel.Contracts.Events.AppendedEvent @event) => new(
            @event.Metadata.ToKernel(),
            @event.Context.ToKernel(),
            JsonSerializer.Deserialize<ExpandoObject>(@event.Content, Globals.JsonSerializerOptions)!);
}
