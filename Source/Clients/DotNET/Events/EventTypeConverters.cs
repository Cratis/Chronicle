// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

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
    public static Kernel.Contracts.Events.EventType ToContract(this EventType type)
    {
        return new()
        {
            Id = type.Id.Value.ToString(),
            Generation = type.Generation.Value,
            IsPublic = type.IsPublic
        };
    }

}
