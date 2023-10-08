// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Services.Events;

/// <summary>
/// Extension methods for converting to and from <see cref="Cratis.Events.EventType"/>.
/// </summary>
public static class EventTypeConverters
{
    /// <summary>
    /// Convert to Kernel representation.
    /// </summary>
    /// <param name="eventType"><see cref="Contracts.Events.EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="Cratis.Events.EventType"/>.</returns>
    public static Cratis.Events.EventType ToKernel(this Contracts.Events.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.IsPublic);
}
